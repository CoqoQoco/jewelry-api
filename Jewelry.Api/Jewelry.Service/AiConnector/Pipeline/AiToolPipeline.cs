using jewelry.Model.AiConnector;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Sensitivity;
using Jewelry.Service.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Pipeline
{
    public sealed class AiToolResult
    {
        public bool IsError { get; set; }
        // JSON ที่ส่งให้ AI (ผลสำเร็จ = ซองข้อมูล, error = ข้อความสั้นภาษาไทย)
        public string Json { get; set; } = "";
        public string? ErrorCode { get; set; }
    }

    public interface IAiToolPipeline
    {
        Task<AiToolResult> Run(ClaimsPrincipal caller, string toolName, string? argumentsJson, CancellationToken ct);
    }

    // ลำดับตายตัว: 1 auth/โหลด user → 2 scope → 3 permission → 4 validate/clamp → 5 cache → 6 execute
    //   → 7 mask (บนสำเนา) → 8 definitions + dataQuality → 9 access log → 10 นับ rate limit
    // (rate limit ตรวจที่ต้นทางทันทีหลัง step 1 เพื่อไม่ให้เสียงานโหลดข้อมูล · นับ/บันทึกที่ step 9-10)
    public class AiToolPipeline : IAiToolPipeline
    {
        public const int DefaultRowCap = 50;
        public const int MaxRowCap = 200;
        public const int MaxRangeDays = 365;

        private static readonly JsonSerializerSettings OutputJson = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Include,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        };

        private readonly AiToolRegistry _registry;
        private readonly IAiUserDirectory _users;
        private readonly InsightCache _cache;
        private readonly IAiGlossaryService _glossary;
        private readonly AiRateLimiter _rateLimiter;
        private readonly JewelryContext _context;
        private readonly ILogger<AiToolPipeline> _logger;
        private readonly IServiceProvider _services;

        public AiToolPipeline(AiToolRegistry registry, IAiUserDirectory users, InsightCache cache, IAiGlossaryService glossary,
            AiRateLimiter rateLimiter, JewelryContext context, ILogger<AiToolPipeline> logger, IServiceProvider services)
        {
            _registry = registry;
            _users = users;
            _cache = cache;
            _glossary = glossary;
            _rateLimiter = rateLimiter;
            _context = context;
            _logger = logger;
            _services = services;
        }

        public async Task<AiToolResult> Run(ClaimsPrincipal caller, string toolName, string? argumentsJson, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var tool = _registry.Find(toolName);

            // rate limit นับต่อ user (ไม่ใช่ต่อ token) กันเปิด token หลายตัวเพื่อเลี่ยงเพดาน
            // 1 auth: principal มาจาก AiConnectorAuthenticationHandler (validate token + user แล้ว) — โหลดสิทธิ์ล่าสุดจาก DB อีกรอบ
            long.TryParse(caller.FindFirst(AiPrincipalFactory.TokenIdClaim)?.Value, out var tokenId);
            int.TryParse(caller.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId);
            var tokenKind = caller.FindFirst(AiPrincipalFactory.TokenKindClaim)?.Value;
            var tokenScopes = caller.FindAll(AiPrincipalFactory.ScopeClaim).Select(c => c.Value).ToList();

            var user = userId > 0 ? await _users.Load(userId) : null;
            if (user == null) return await Fail(null, userId, tokenId, toolName, tokenScopes.Where(s => !AiAccessRules.IsDataScope(s)).ToList(), null, sw, AiErrorCodes.Unauthorized);

            // scope ที่ใช้จริง: MODULE จาก token · DATA จาก grant ของ user เฉพาะ token OAUTH
            var scopes = await AiAccessRules.EffectiveScopes(_context, userId, tokenKind, tokenScopes);

            if (await AiAccessRules.IsSuspended(_context, userId)) return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.Suspended);

            if (!_rateLimiter.TryAcquire(userId)) return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.RateLimited);

            if (tool == null) return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.UnknownTool);

            var context = new AiToolContext
            {
                UserId = user.UserId,
                Username = user.Username,
                TokenId = tokenId,
                Roles = user.Roles,
                Services = _services,
                Scopes = new HashSet<string>(scopes),
                Permissions = user.Permissions,
            };

            // 2 scope
            if (tool.RequiredScopes.Any(s => !context.Scopes.Contains(s)))
                return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.ScopeDenied);

            // 3 permission (ANY)
            if (!tool.PermissionsAny.Any(p => context.Permissions.Contains(p)))
                return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.PermissionDenied);

            // 4 validate / clamp input
            object input;
            string inputJson;
            try
            {
                input = ParseInput(tool, argumentsJson);
                Clamp(input);
                inputJson = JsonConvert.SerializeObject(input);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                return await Fail(user, userId, tokenId, toolName, scopes, null, sw, AiErrorCodes.InvalidInput);
            }

            try
            {
                // 5 cache (เก็บผลดิบที่ยังไม่ mask — ห้ามแก้ object ที่ได้จาก cache)
                object raw;
                if (tool.CacheTtl > TimeSpan.Zero)
                {
                    var key = $"ai:{tool.Name}:{tool.RowFilterId(context)}:{AiTokenCrypto.Sha256Hex(inputJson).Substring(0, 16)}";
                    raw = await _cache.GetOrCreateAsync<object>(key, tool.CacheTtl, () => tool.ExecuteAsync(input, context, ct));
                }
                else
                {
                    // 6 execute
                    raw = await tool.ExecuteAsync(input, context, ct);
                }

                // 7 mask บนสำเนา
                var (masked, maskedCount, notes) = MaskOutput(raw, context);

                // 8 definitions + dataQuality
                var glossary = await _glossary.GetByCodes(tool.MetricCodes.ToList());
                var envelope = new Dictionary<string, object?>
                {
                    ["data"] = masked,
                    ["definitions"] = glossary.Select(g => new { metric = g.MetricCode, name = g.NameTh, definition = g.DefinitionTh }).ToList(),
                    ["dataQuality"] = glossary.Where(g => !string.IsNullOrEmpty(g.CaveatTh)).Select(g => new { metric = g.MetricCode, caveat = g.CaveatTh }).ToList(),
                    ["notes"] = notes,
                };
                var rowCount = CountRows(masked);

                // 9 access log (10 นับ rate limit ทำไปแล้วตอน TryAcquire)
                await WriteLog(userId, tokenId, tool.Name, scopes, inputJson, rowCount, maskedCount, sw, null);
                return new AiToolResult { Json = JsonConvert.SerializeObject(envelope, OutputJson) };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI tool {Tool} failed", tool.Name);
                return await Fail(user, userId, tokenId, toolName, scopes, inputJson, sw, AiErrorCodes.ToolError);
            }
        }

        private static (object Value, int MaskedCount, List<string> Notes) MaskOutput(object raw, AiToolContext context)
        {
            bool Allowed(SensitiveCategory c) => SensitiveCategoryPolicy.IsAllowed(c, context.Scopes, context.Permissions);
            var method = typeof(AiMasker).GetMethod(nameof(AiMasker.Mask))!.MakeGenericMethod(raw.GetType());
            var result = method.Invoke(null, new object[] { raw, (Func<SensitiveCategory, bool>)Allowed })!;
            var type = result.GetType();
            return (type.GetProperty("Value")!.GetValue(result)!, (int)type.GetProperty("MaskedCount")!.GetValue(result)!, (List<string>)type.GetProperty("Notes")!.GetValue(result)!);
        }

        private static object ParseInput(IAiTool tool, string? argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson)) return Activator.CreateInstance(tool.InputType)!;
            return JsonConvert.DeserializeObject(argumentsJson, tool.InputType, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Ignore })
                ?? Activator.CreateInstance(tool.InputType)!;
        }

        public static void Clamp(object input)
        {
            if (input is IAiRangedInput ranged)
            {
                var to = ranged.To ?? DateTime.UtcNow;
                var from = ranged.From ?? to.AddDays(-30);
                if (from > to) throw new ArgumentException("ช่วงวันที่ไม่ถูกต้อง");
                if ((to - from).TotalDays > MaxRangeDays) from = to.AddDays(-MaxRangeDays);
                ranged.From = from;
                ranged.To = to;
            }
            if (input is IAiRowCappedInput capped)
            {
                var limit = capped.Limit ?? DefaultRowCap;
                capped.Limit = Math.Clamp(limit <= 0 ? DefaultRowCap : limit, 1, MaxRowCap);
            }
        }

        private static int CountRows(object output) =>
            output.GetType().GetProperties()
                .Where(p => typeof(IList).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                .Sum(p => (p.GetValue(output) as IList)?.Count ?? 0);

        private async Task<AiToolResult> Fail(AiUserInfo? user, int userId, long tokenId, string toolName, List<string> scopes,
            string? inputJson, Stopwatch sw, string errorCode)
        {
            if (userId > 0) await WriteLog(userId, tokenId, toolName, scopes, inputJson, 0, 0, sw, errorCode);
            return new AiToolResult
            {
                IsError = true,
                ErrorCode = errorCode,
                Json = JsonConvert.SerializeObject(new { error = errorCode, message = AiErrorMessages.Thai(errorCode) }, OutputJson),
            };
        }

        private async Task WriteLog(int userId, long tokenId, string tool, List<string> scopes, string? paramsJson,
            int rowCount, int maskedCount, Stopwatch sw, string? errorCode)
        {
            try
            {
                _context.TbtAiAccessLog.Add(new TbtAiAccessLog
                {
                    TokenId = tokenId > 0 ? tokenId : null,
                    UserId = userId,
                    Tool = tool.Length > 100 ? tool.Substring(0, 100) : tool,
                    Scopes = AiScopes.Join(scopes),
                    Params = string.IsNullOrEmpty(paramsJson) ? null : paramsJson,
                    RowCount = rowCount,
                    MaskedCount = maskedCount,
                    DurationMs = (int)sw.ElapsedMilliseconds,
                    ErrorCode = errorCode,
                    CreateDate = DateTime.UtcNow,
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI access log write failed");
            }
        }
    }
}
