using jewelry.Model.AiConnector;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public sealed class AiClient
    {
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string Kind { get; set; } = "";
        public List<string> RedirectUris { get; set; } = new List<string>();
        public string? MetadataUrl { get; set; }
        // redirect_uris ทั้งหมดอยู่ใน allowlist ที่ระบบรู้จัก (Claude/ChatGPT/loopback/app scheme)
        public bool Verified => RedirectUris.Count > 0 && RedirectUris.All(SsrfGuard.IsKnownRedirect);
    }

    public interface IClientMetadataFetcher
    {
        // คืน JSON ของ client metadata — throw ถ้า fetch ไม่ได้/ไม่ปลอดภัย
        Task<string> Fetch(Uri uri, CancellationToken ct);
    }

    // fetch ด้วย SocketsHttpHandler.ConnectCallback เพื่อตรวจ IP "ตอนเชื่อมต่อจริง" (กัน DNS rebinding) · ไม่ตาม redirect · จำกัด 64KB / 5 วินาที
    public class ClientMetadataFetcher : IClientMetadataFetcher
    {
        private const int MaxBytes = 64 * 1024;

        private static readonly HttpClient Http = new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            ConnectTimeout = TimeSpan.FromSeconds(4),
            ConnectCallback = async (ctx, ct) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(ctx.DnsEndPoint.Host, ct);
                if (addresses.Length == 0 || addresses.Any(a => !SsrfGuard.IsPublicAddress(a)))
                    throw new InvalidOperationException("client metadata host ไม่ใช่ public address");
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, ctx.DnsEndPoint.Port, ct);
                    return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        })
        { Timeout = TimeSpan.FromSeconds(5) };

        public async Task<string> Fetch(Uri uri, CancellationToken ct)
        {
            if (!SsrfGuard.IsAllowedMetadataUrl(uri.ToString(), out _)) throw new InvalidOperationException("client_id URL ไม่ปลอดภัย");
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.Accept.ParseAdd("application/json");
            using var res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            if (res.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("client metadata ตอบ " + (int)res.StatusCode);
            if (res.Content.Headers.ContentLength > MaxBytes) throw new InvalidOperationException("client metadata ใหญ่เกินไป");

            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            var buffer = new byte[MaxBytes + 1];
            var total = 0;
            int n;
            while ((n = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), ct)) > 0)
            {
                total += n;
                if (total > MaxBytes) throw new InvalidOperationException("client metadata ใหญ่เกินไป");
            }
            return System.Text.Encoding.UTF8.GetString(buffer, 0, total);
        }
    }

    public interface IAiClientRegistry
    {
        // allowStored = ยอมใช้แถวที่บันทึกไว้เมื่อ cache หมดและ fetch ไม่ได้ (ใช้ตอนแลก token เท่านั้น ไม่ใช้ตอน authorize)
        Task<AiClient?> Resolve(string? clientId, CancellationToken ct = default, bool allowStored = false);
        // บันทึก CIMD client ลง DB หลัง user กดอนุญาตสำเร็จเท่านั้น (DCR บันทึกตอนลงทะเบียนอยู่แล้ว)
        Task Persist(AiClient client);
        Task<DcrResponse> Register(DcrRequest request);
    }

    public class AiClientRegistry : IAiClientRegistry
    {
        private static readonly TimeSpan CimdCacheTtl = TimeSpan.FromHours(1);
        private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromMinutes(2);

        private readonly JewelryContext _context;
        private readonly IClientMetadataFetcher _fetcher;
        private readonly IMemoryCache _cache;

        public AiClientRegistry(JewelryContext context, IClientMetadataFetcher fetcher, IMemoryCache cache)
        {
            _context = context;
            _fetcher = fetcher;
            _cache = cache;
        }

        public async Task<AiClient?> Resolve(string? clientId, CancellationToken ct = default, bool allowStored = false)
        {
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 2048) return null;

            if (clientId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return await ResolveCimd(clientId, ct, allowStored);

            var row = await _context.TbtAiClient.FirstOrDefaultAsync(c => c.ClientId == clientId && c.Kind == "DCR", ct);
            if (row == null) return null;
            return ToClient(row);
        }

        private async Task<AiClient?> ResolveCimd(string clientId, CancellationToken ct, bool allowStored)
        {
            var cacheKey = "ai:cimd:" + clientId;
            if (_cache.TryGetValue(cacheKey, out AiClient? cached) && cached != null) return cached;
            if (_cache.TryGetValue("ai:cimd:neg:" + clientId, out _)) return await StoredOrNull(clientId, allowStored, ct);

            if (!SsrfGuard.IsAllowedMetadataUrl(clientId, out var uri) || uri == null) return null;

            AiClient? client = null;
            try
            {
                var json = await _fetcher.Fetch(uri, ct);
                client = ParseMetadata(clientId, json);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is HttpRequestException || ex is TaskCanceledException || ex is SocketException || ex is JsonException)
            {
                client = null;
            }

            if (client == null)
            {
                // negative cache กัน client เจ้าเดิมวนยิง fetch ซ้ำ ๆ
                _cache.Set("ai:cimd:neg:" + clientId, true, NegativeCacheTtl);
                return await StoredOrNull(clientId, allowStored, ct);
            }

            _cache.Set(cacheKey, client, CimdCacheTtl);
            return client;
        }

        private async Task<AiClient?> StoredOrNull(string clientId, bool allowStored, CancellationToken ct)
        {
            if (!allowStored) return null;
            var row = await _context.TbtAiClient.AsNoTracking().FirstOrDefaultAsync(c => c.ClientId == clientId && c.Kind == "CIMD", ct);
            return row == null ? null : ToClient(row);
        }

        public async Task Persist(AiClient client)
        {
            if (client.Kind != "CIMD") return;
            var now = DateTime.UtcNow;
            var row = await _context.TbtAiClient.FirstOrDefaultAsync(c => c.ClientId == client.ClientId);
            if (row == null)
            {
                row = new TbtAiClient { ClientId = client.ClientId, Kind = "CIMD", CreateDate = now };
                _context.TbtAiClient.Add(row);
            }
            row.ClientName = client.ClientName;
            row.RedirectUris = JsonConvert.SerializeObject(client.RedirectUris);
            row.MetadataUrl = client.ClientId;
            row.LastSeen = now;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // ชน unique (client_id) จาก request พร้อมกัน — อ่านแถวที่มีอยู่แล้วอัปเดต last_seen
                _context.Entry(row).State = EntityState.Detached;
                var existing = await _context.TbtAiClient.FirstOrDefaultAsync(c => c.ClientId == client.ClientId);
                if (existing == null) throw;
                existing.ClientName = client.ClientName;
                existing.RedirectUris = JsonConvert.SerializeObject(client.RedirectUris);
                existing.LastSeen = now;
                _context.TbtAiClient.Update(existing);
                await _context.SaveChangesAsync();
            }
        }

        // ตรวจชนิด JSON ทุกช่อง — ชนิดผิด = null (ไม่ throw) เพื่อให้ตอบ invalid_client แทน 500
        public static AiClient? ParseMetadata(string clientId, string json)
        {
            JObject doc;
            try { doc = JObject.Parse(json); }
            catch (JsonException) { return null; }

            if (doc["client_id"]?.Type != JTokenType.String || !string.Equals((string?)doc["client_id"], clientId, StringComparison.Ordinal)) return null;
            var authToken = doc["token_endpoint_auth_method"];
            if (authToken != null && authToken.Type != JTokenType.Null)
            {
                if (authToken.Type != JTokenType.String || (string?)authToken != "none") return null;
            }

            if (doc["client_name"]?.Type != JTokenType.String) return null;
            var name = ((string?)doc["client_name"])?.Trim();
            if (string.IsNullOrEmpty(name)) return null;
            if (name.Length > 100) name = name.Substring(0, 100);

            if (doc["redirect_uris"] is not JArray arr || arr.Count == 0 || arr.Count > 10) return null;
            if (arr.Any(t => t.Type != JTokenType.String)) return null;
            var uris = arr.Select(t => (string)t!).ToList();
            if (uris.Any(u => !SsrfGuard.IsValidRedirectUri(u))) return null;

            // https redirect ต้อง origin เดียวกับ client_id (ยกเว้น loopback และ app scheme)
            var idUri = new Uri(clientId);
            foreach (var u in uris)
            {
                var r = new Uri(u);
                if (r.Scheme != Uri.UriSchemeHttps) continue;
                if (!string.Equals(r.GetLeftPart(UriPartial.Authority), idUri.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) return null;
            }

            return new AiClient { ClientId = clientId, ClientName = name, Kind = "CIMD", RedirectUris = uris, MetadataUrl = clientId };
        }

        public async Task<DcrResponse> Register(DcrRequest request)
        {
            var name = request.ClientName?.Trim();
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("client_name จำเป็นต้องระบุ");
            if (name.Length > 100) name = name.Substring(0, 100);

            var uris = request.RedirectUris ?? new List<string>();
            if (uris.Count == 0 || uris.Count > 10 || uris.Any(u => !SsrfGuard.IsValidRedirectUri(u)))
                throw new ArgumentException("redirect_uris ไม่ถูกต้อง");
            if (!string.IsNullOrEmpty(request.TokenEndpointAuthMethod) && request.TokenEndpointAuthMethod != "none")
                throw new ArgumentException("รองรับเฉพาะ public client (token_endpoint_auth_method=none)");

            var clientId = "dkc_" + AiTokenCrypto.RandomToken(18);
            _context.TbtAiClient.Add(new TbtAiClient
            {
                ClientId = clientId,
                ClientName = name,
                Kind = "DCR",
                RedirectUris = JsonConvert.SerializeObject(uris.Distinct().ToList()),
                CreateDate = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            return new DcrResponse { ClientId = clientId, ClientName = name, RedirectUris = uris.Distinct().ToList() };
        }

        private static AiClient ToClient(TbtAiClient row) => new AiClient
        {
            ClientId = row.ClientId,
            ClientName = row.ClientName,
            Kind = row.Kind,
            RedirectUris = JsonConvert.DeserializeObject<List<string>>(row.RedirectUris) ?? new List<string>(),
            MetadataUrl = row.MetadataUrl,
        };
    }
}
