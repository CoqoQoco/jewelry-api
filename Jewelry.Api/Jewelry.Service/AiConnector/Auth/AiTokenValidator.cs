using Jewelry.Data.Context;
using jewelry.Model.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public sealed class AiValidatedToken
    {
        public long TokenId { get; set; }
        public string Kind { get; set; } = "";
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Scopes { get; set; } = new List<string>();
    }

    public interface IAiTokenValidator
    {
        // null = ใช้ไม่ได้ (ไม่รู้จัก/หมดอายุ/ถูก revoke/user ถูกปิดหรือถูกถอดสิทธิ์ ai:connect)
        Task<AiValidatedToken?> Validate(string? bearer);
    }

    public class AiTokenValidator : IAiTokenValidator
    {
        private readonly JewelryContext _context;
        private readonly IAiUserDirectory _users;
        private readonly AiConnectorOptions _options;
        private readonly IAiTokenStore _store;

        public AiTokenValidator(JewelryContext context, IAiUserDirectory users, IOptions<AiConnectorOptions> options, IAiTokenStore store)
        {
            _store = store;
            _context = context;
            _users = users;
            _options = options.Value;
        }

        public async Task<AiValidatedToken?> Validate(string? bearer)
        {
            if (!_options.IsConfigured || string.IsNullOrWhiteSpace(bearer)) return null;

            long tokenId;
            string? patHash = null;
            if (bearer.StartsWith(AiTokenKinds.PatPrefix, StringComparison.Ordinal))
            {
                patHash = AiTokenCrypto.Sha256Hex(bearer);
                tokenId = 0;
            }
            else
            {
                var parsed = ParseJwt(bearer);
                if (parsed == null) return null;
                tokenId = parsed.Value;
            }

            var now = DateTime.UtcNow;
            var row = patHash != null
                ? await _context.TbtAiToken.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == patHash && t.Kind == AiTokenKinds.Pat)
                : await _context.TbtAiToken.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tokenId && t.Kind == AiTokenKinds.OAuth);
            if (row == null || row.RevokedAt != null || row.ExpiresAt <= now) return null;

            var user = await _users.Load(row.UserId);
            if (user == null || !user.Permissions.Contains(PermissionCodes.AiConnect)) return null;

            if (row.LastUsedAt == null || now - row.LastUsedAt.Value > TimeSpan.FromMinutes(1))
            {
                await _store.TouchLastUsed(row.Id, now);
            }

            return new AiValidatedToken
            {
                TokenId = row.Id,
                Kind = row.Kind,
                UserId = user.UserId,
                Username = user.Username,
                Roles = user.Roles,
                Scopes = AiScopes.Split(row.Scopes).ToList(),
            };
        }

        // key/aud/iss แยกจาก JWT ของเว็บโดยสิ้นเชิง — JWT เว็บไม่ผ่านที่นี่
        private long? ParseJwt(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey!)),
                    ValidateIssuer = true,
                    ValidIssuer = _options.EffectiveIssuer,
                    ValidateAudience = true,
                    ValidAudience = AiConnectorOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                }, out _);
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                return long.TryParse(jti, out var id) ? id : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public static class AiPrincipalFactory
    {
        public const string AuthenticationType = "AiConnector";
        public const string TokenIdClaim = "ai_token_id";
        public const string ScopeClaim = "ai_scope";
        public const string TokenKindClaim = "ai_token_kind";

        public static ClaimsPrincipal Create(AiValidatedToken token)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, token.UserId.ToString()),
                new Claim(ClaimTypes.Name, token.Username),
                new Claim(TokenIdClaim, token.TokenId.ToString()),
                new Claim(TokenKindClaim, token.Kind),
            };
            claims.AddRange(token.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
            claims.AddRange(token.Scopes.Select(s => new Claim(ScopeClaim, s)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType));
        }
    }
}
