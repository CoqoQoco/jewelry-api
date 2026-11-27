using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector.Sensitivity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public sealed class AiOAuthException : Exception
    {
        public AiOAuthException(string error, string description, bool redirectable = false) : base(description)
        {
            Error = error;
            Description = description;
            Redirectable = redirectable;
        }

        public string Error { get; }
        public string Description { get; }
        // true = ปลอดภัยที่จะ redirect error กลับไปที่ client (client + redirect_uri ผ่านแล้ว)
        public bool Redirectable { get; }
    }

    public interface IAiOAuthService
    {
        // ตรวจ authorize request (client / redirect_uri / PKCE / scope) — throw AiOAuthException ถ้าไม่ผ่าน
        Task ValidateAuthorizeRequest(AuthorizeParams request, string expectedResource, CancellationToken ct = default);
        Task<ConsentResponse> GetConsent(AuthorizeParams request, int userId, string expectedResource, CancellationToken ct = default);
        Task<ApproveResponse> Approve(ApproveRequest request, int userId, string expectedResource, CancellationToken ct = default);
        Task<ApproveResponse> Deny(AuthorizeParams request, string expectedResource, CancellationToken ct = default);
        Task<TokenResponse> Exchange(TokenRequest request, string expectedResource, CancellationToken ct = default);
    }

    public class OAuthService : IAiOAuthService
    {
        private readonly JewelryContext _context;
        private readonly IAiClientRegistry _clients;
        private readonly IAiScopeCatalog _scopes;
        private readonly IAiUserDirectory _users;
        private readonly AiConnectorOptions _options;
        private readonly IAiTokenStore _store;

        public OAuthService(JewelryContext context, IAiClientRegistry clients, IAiScopeCatalog scopes,
            IAiUserDirectory users, IOptions<AiConnectorOptions> options, IAiTokenStore store)
        {
            _store = store;
            _context = context;
            _clients = clients;
            _scopes = scopes;
            _users = users;
            _options = options.Value;
        }

        private sealed class Validated
        {
            public AiClient Client { get; set; } = null!;
            public List<string> RequestedScopes { get; set; } = new List<string>();
            public List<TbmAiScope> Catalog { get; set; } = new List<TbmAiScope>();
        }

        private async Task<Validated> ValidateAuthorize(AuthorizeParams p, string expectedResource, CancellationToken ct)
        {
            if (!_options.IsConfigured) throw new AiOAuthException("server_error", "AI connector ยังไม่ได้ตั้งค่า");

            var client = await _clients.Resolve(p.ClientId, ct)
                ?? throw new AiOAuthException("invalid_client", "ไม่พบหรือไม่สามารถตรวจสอบ AI ที่ขอเชื่อมต่อ");
            if (string.IsNullOrEmpty(p.RedirectUri) || !SsrfGuard.RedirectMatches(p.RedirectUri, client.RedirectUris))
                throw new AiOAuthException("invalid_request", "redirect_uri ไม่ตรงกับที่ลงทะเบียนไว้");

            if (p.ResponseType != "code")
                throw new AiOAuthException("unsupported_response_type", "รองรับเฉพาะ response_type=code", client.Verified);
            if (string.IsNullOrEmpty(p.CodeChallenge) || p.CodeChallengeMethod != "S256" || p.CodeChallenge.Length < 43 || p.CodeChallenge.Length > 128)
                throw new AiOAuthException("invalid_request", "ต้องใช้ PKCE (code_challenge_method=S256)", client.Verified);
            if (!string.IsNullOrEmpty(p.Resource) && !string.Equals(p.Resource.TrimEnd('/'), expectedResource.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                throw new AiOAuthException("invalid_target", "resource ไม่ถูกต้อง", client.Verified);

            var catalog = await _scopes.Active();
            var requested = AiScopes.Split(p.Scope).Distinct().ToList();
            if (requested.Any(s => catalog.All(c => c.Code != s)))
                throw new AiOAuthException("invalid_scope", "มี scope ที่ไม่รู้จัก", client.Verified);
            if (!requested.Any(s => catalog.First(c => c.Code == s).Kind == "MODULE"))
                requested.Add(AiScopes.ModuleExecutive);

            return new Validated { Client = client, RequestedScopes = requested, Catalog = catalog };
        }

        public async Task ValidateAuthorizeRequest(AuthorizeParams request, string expectedResource, CancellationToken ct = default)
            => await ValidateAuthorize(request, expectedResource, ct);

        public async Task<ConsentResponse> GetConsent(AuthorizeParams request, int userId, string expectedResource, CancellationToken ct = default)
        {
            var v = await ValidateAuthorize(request, expectedResource, ct);
            var user = await _users.Load(userId);
            var canApprove = user != null && user.Permissions.Contains(PermissionCodes.AiConnect);

            var response = new ConsentResponse
            {
                CanApprove = canApprove,
                DenyReasonTh = canApprove ? null : "บัญชีนี้ไม่มีสิทธิ์เชื่อมต่อ AI (ai:connect) กรุณาติดต่อผู้ดูแลระบบ",
                ClientId = v.Client.ClientId,
                ClientName = v.Client.ClientName,
                ClientKind = v.Client.Kind,
                ClientVerified = v.Client.Verified,
                RedirectUri = request.RedirectUri!,
                RedirectUris = v.Client.RedirectUris.ToList(),
                RedirectHost = new Uri(request.RedirectUri!).Host,
                UserDisplayName = user?.DisplayName ?? "",
            };

            foreach (var s in v.Catalog)
            {
                var required = s.Kind == "MODULE" && v.RequestedScopes.Contains(s.Code);
                // MODULE ที่ client ไม่ได้ขอ แสดงเป็น checkbox เสริม (ไม่ติ๊กเป็นค่าเริ่มต้น) เหมือน DATA
                if (s.Kind != "MODULE" && s.Kind != "DATA") continue;

                var available = true;
                string? reason = null;
                if (s.Kind == "DATA")
                {
                    var rule = SensitiveCategoryPolicy.ForScope(s.Code);
                    available = rule != null && user != null && user.Permissions.Contains(rule.Permission);
                    if (!available) reason = "บัญชีนี้ไม่มีสิทธิ์ดูข้อมูลหมวดนี้";
                }

                response.Scopes.Add(new ConsentScope
                {
                    Code = s.Code,
                    NameTh = s.NameTh,
                    DescriptionTh = s.DescriptionTh,
                    Kind = s.Kind,
                    Required = required,
                    DefaultChecked = required,
                    Available = available,
                    UnavailableReasonTh = reason,
                });
            }
            return response;
        }

        public async Task<ApproveResponse> Approve(ApproveRequest request, int userId, string expectedResource, CancellationToken ct = default)
        {
            var v = await ValidateAuthorize(request, expectedResource, ct);
            var user = await _users.Load(userId);
            if (user == null || !user.Permissions.Contains(PermissionCodes.AiConnect))
                throw new AiOAuthException("access_denied", "บัญชีนี้ไม่มีสิทธิ์เชื่อมต่อ AI");

            var approved = request.ApprovedScopes.Distinct().ToList();
            foreach (var s in approved)
            {
                var entry = v.Catalog.FirstOrDefault(c => c.Code == s);
                if (entry == null) throw new AiOAuthException("invalid_scope", "scope ไม่ถูกต้อง");
                if (entry.Kind == "DATA")
                {
                    var rule = SensitiveCategoryPolicy.ForScope(s);
                    if (rule == null || !user.Permissions.Contains(rule.Permission))
                        throw new AiOAuthException("invalid_scope", "บัญชีนี้ไม่มีสิทธิ์ให้ scope " + s);
                }
                else if (entry.Kind != "MODULE")
                {
                    throw new AiOAuthException("invalid_scope", "scope ไม่ถูกต้อง");
                }
            }
            var requiredModules = v.RequestedScopes.Where(s => v.Catalog.First(c => c.Code == s).Kind == "MODULE");
            if (requiredModules.Any(m => !approved.Contains(m)))
                throw new AiOAuthException("invalid_scope", "ต้องอนุญาต scope หลักที่ AI ขอ");

            var code = AiTokenCrypto.RandomToken(32);
            _context.TbtAiAuthCode.Add(new TbtAiAuthCode
            {
                CodeHash = AiTokenCrypto.Sha256Hex(code),
                ClientId = v.Client.ClientId,
                UserId = userId,
                RedirectUri = request.RedirectUri!,
                CodeChallenge = request.CodeChallenge!,
                Scopes = AiScopes.Join(approved),
                Resource = string.IsNullOrEmpty(request.Resource) ? null : request.Resource,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.AuthCodeMinutes),
            });
            await _context.SaveChangesAsync(ct);
            await _clients.Persist(v.Client);

            return new ApproveResponse { RedirectUrl = BuildRedirect(request.RedirectUri!, ("code", code), ("state", request.State)) };
        }

        public async Task<ApproveResponse> Deny(AuthorizeParams request, string expectedResource, CancellationToken ct = default)
        {
            var v = await ValidateAuthorize(request, expectedResource, ct);
            // client ที่ระบบไม่รู้จักห้ามส่ง user ไป redirect_uri แม้เป็น error — UI แสดง "ปฏิเสธแล้ว" ในหน้าเอง
            if (!v.Client.Verified) return new ApproveResponse { RedirectUrl = null };
            return new ApproveResponse { RedirectUrl = BuildRedirect(request.RedirectUri!, ("error", "access_denied"), ("state", request.State)) };
        }

        public static string BuildRedirect(string redirectUri, params (string Key, string? Value)[] query)
        {
            var parts = query.Where(q => !string.IsNullOrEmpty(q.Value))
                .Select(q => Uri.EscapeDataString(q.Key) + "=" + Uri.EscapeDataString(q.Value!));
            var sep = redirectUri.Contains('?') ? "&" : "?";
            return redirectUri + sep + string.Join("&", parts);
        }

        public async Task<TokenResponse> Exchange(TokenRequest request, string expectedResource, CancellationToken ct = default)
        {
            if (!_options.IsConfigured) throw new AiOAuthException("server_error", "AI connector ยังไม่ได้ตั้งค่า");
            return request.GrantType switch
            {
                "authorization_code" => await ExchangeCode(request, expectedResource, ct),
                "refresh_token" => await ExchangeRefresh(request, ct),
                _ => throw new AiOAuthException("unsupported_grant_type", "รองรับเฉพาะ authorization_code และ refresh_token"),
            };
        }

        private async Task<TokenResponse> ExchangeCode(TokenRequest r, string expectedResource, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(r.Code)) throw new AiOAuthException("invalid_request", "ไม่มี code");
            var now = DateTime.UtcNow;
            var hash = AiTokenCrypto.Sha256Hex(r.Code);
            var code = await _context.TbtAiAuthCode.AsNoTracking().FirstOrDefaultAsync(c => c.CodeHash == hash, ct);
            if (code == null) throw new AiOAuthException("invalid_grant", "code ไม่ถูกต้อง");

            var familyId = "f_" + hash.Substring(0, 24);
            // พิสูจน์ก่อนว่าผู้เรียกครอบครอง code + verifier จริง (หมดอายุ/client/redirect/PKCE) ค่อยตัดสินว่า "ใช้ซ้ำ" แล้ว revoke family
            // — คนที่เห็นแค่ code เก่าจะยิงทำลายการเชื่อมต่อของคนอื่นไม่ได้
            if (code.ExpiresAt <= now) throw new AiOAuthException("invalid_grant", "code หมดอายุ");
            if (code.ClientId != r.ClientId || code.RedirectUri != r.RedirectUri)
                throw new AiOAuthException("invalid_grant", "client หรือ redirect_uri ไม่ตรงกับตอนขอ code");
            if (!AiTokenCrypto.VerifyPkceS256(r.CodeVerifier, code.CodeChallenge))
                throw new AiOAuthException("invalid_grant", "code_verifier ไม่ถูกต้อง");
            if (!string.IsNullOrEmpty(code.Resource) && !string.IsNullOrEmpty(r.Resource)
                && !string.Equals(code.Resource.TrimEnd('/'), r.Resource.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                throw new AiOAuthException("invalid_target", "resource ไม่ตรงกับตอนขอ code");

            if (code.UsedAt != null)
            {
                await _store.KillFamily(familyId, now, ct);
                throw new AiOAuthException("invalid_grant", "code ถูกใช้ไปแล้ว");
            }

            var client = await _clients.Resolve(code.ClientId, ct, allowStored: true) ?? throw new AiOAuthException("invalid_client", "ไม่พบ client");
            var user = await _users.Load(code.UserId);
            if (user == null || !user.Permissions.Contains(PermissionCodes.AiConnect))
                throw new AiOAuthException("invalid_grant", "บัญชีไม่มีสิทธิ์เชื่อมต่อ AI แล้ว");

            // atomic: UPDATE ... WHERE code_hash=@h AND used_at IS NULL ต้องได้ 1 แถว — คนที่แพ้ race ถือว่าใช้ code ซ้ำ
            if (!await _store.ClaimAuthCode(hash, now, ct))
            {
                await _store.KillFamily(familyId, now, ct);
                throw new AiOAuthException("invalid_grant", "code ถูกใช้ไปแล้ว");
            }

            var response = await Issue(user, client.ClientId, client.ClientName, code.Scopes, familyId, null, now, ct);

            // family ถูก kill ระหว่างออก token (เช่น code ถูกใช้ซ้ำพร้อมกัน) → revoke แถวใหม่ทันที
            if (await _store.IsFamilyKilled(familyId, ct))
            {
                await _store.KillFamily(familyId, DateTime.UtcNow, ct);
                throw new AiOAuthException("invalid_grant", "code ถูกใช้ไปแล้ว");
            }
            return response;
        }

        private async Task<TokenResponse> ExchangeRefresh(TokenRequest r, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(r.RefreshToken)) throw new AiOAuthException("invalid_request", "ไม่มี refresh_token");
            if (string.IsNullOrEmpty(r.ClientId)) throw new AiOAuthException("invalid_request", "ไม่มี client_id");
            var now = DateTime.UtcNow;
            var hash = AiTokenCrypto.Sha256Hex(r.RefreshToken);
            var row = await _context.TbtAiToken.AsNoTracking().FirstOrDefaultAsync(t => t.RefreshHash == hash && t.Kind == AiTokenKinds.OAuth, ct);
            if (row == null) throw new AiOAuthException("invalid_grant", "refresh_token ไม่ถูกต้อง");
            if (r.ClientId != row.ClientId) throw new AiOAuthException("invalid_grant", "client ไม่ตรงกัน");

            var familyId = row.FamilyId ?? ("f_" + row.Id);
            if (row.RevokedAt != null)
            {
                // ใช้ refresh token ที่ถูกหมุน/ยกเลิกไปแล้วซ้ำ = สงสัยถูกขโมย → revoke ทั้ง family
                await _store.KillFamily(familyId, now, ct);
                throw new AiOAuthException("invalid_grant", "refresh_token ถูกใช้ไปแล้ว");
            }
            if (row.RefreshExpiresAt == null || row.RefreshExpiresAt <= now) throw new AiOAuthException("invalid_grant", "refresh_token หมดอายุ");
            if (await _store.IsFamilyKilled(familyId, ct)) throw new AiOAuthException("invalid_grant", "การเชื่อมต่อถูกยกเลิกแล้ว");

            var user = await _users.Load(row.UserId);
            if (user == null || !user.Permissions.Contains(PermissionCodes.AiConnect))
                throw new AiOAuthException("invalid_grant", "บัญชีไม่มีสิทธิ์เชื่อมต่อ AI แล้ว");

            // atomic: UPDATE ... WHERE id=@id AND revoked_at IS NULL ต้องได้ 1 แถว — แพ้ race = ใช้ refresh ซ้ำ
            if (!await _store.ClaimRefresh(row.Id, now, ct))
            {
                await _store.KillFamily(familyId, now, ct);
                throw new AiOAuthException("invalid_grant", "refresh_token ถูกใช้ไปแล้ว");
            }

            var response = await Issue(user, row.ClientId!, row.Name, row.Scopes, familyId, row.LastUsedAt, now, ct);

            // กันกรณี family ถูก revoke ระหว่างที่กำลังออก token ใหม่ (revoke รอบนั้นไม่เห็นแถวใหม่)
            if (await _store.IsFamilyKilled(familyId, ct))
            {
                await _store.KillFamily(familyId, DateTime.UtcNow, ct);
                throw new AiOAuthException("invalid_grant", "การเชื่อมต่อถูกยกเลิกแล้ว");
            }
            return response;
        }

        private async Task<TokenResponse> Issue(AiUserInfo user, string clientId, string clientName, string scopes,
            string familyId, DateTime? lastUsedAt, DateTime now, CancellationToken ct)
        {
            var refresh = AiTokenCrypto.RandomToken(32);
            var row = new TbtAiToken
            {
                Kind = AiTokenKinds.OAuth,
                UserId = user.UserId,
                ClientId = clientId,
                Name = clientName,
                Scopes = scopes,
                RefreshHash = AiTokenCrypto.Sha256Hex(refresh),
                FamilyId = familyId,
                ExpiresAt = now.AddMinutes(_options.AccessTokenMinutes),
                RefreshExpiresAt = now.AddDays(_options.RefreshTokenDays),
                LastUsedAt = lastUsedAt,
                CreateDate = now,
            };
            _context.TbtAiToken.Add(row);
            await _context.SaveChangesAsync(ct);

            return new TokenResponse
            {
                AccessToken = CreateAccessToken(row, now),
                ExpiresIn = _options.AccessTokenMinutes * 60,
                RefreshToken = refresh,
                Scope = scopes,
            };
        }

        public string CreateAccessToken(TbtAiToken row, DateTime now)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!));
            var token = new JwtSecurityToken(
                issuer: _options.EffectiveIssuer,
                audience: AiConnectorOptions.Audience,
                claims: new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, row.UserId.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, row.Id.ToString()),
                    new Claim("scope", row.Scopes),
                },
                notBefore: now,
                expires: row.ExpiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
