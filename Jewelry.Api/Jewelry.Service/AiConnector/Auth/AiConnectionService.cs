using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector.Registry;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public interface IAiConnectionService
    {
        Task<ConnectionListResponse> List(int userId);
        Task Revoke(int userId, long id);
        Task<CreatePatResponse> CreatePat(int userId, CreatePatRequest request);
        Task<AccessLogResponse> AccessLog(int userId, AccessLogRequest request);
        Task<List<PatScopeOption>> PatScopeOptions();
    }

    public class AiConnectionService : IAiConnectionService
    {
        private static readonly int[] PatDays = { 30, 60, 90 };

        private readonly JewelryContext _context;
        private readonly IAiUserDirectory _users;
        private readonly IAiScopeCatalog _scopes;
        private readonly AiToolRegistry _registry;
        private readonly IAiTokenStore _store;
        public const int MaxActivePatsPerUser = 5;

        public AiConnectionService(JewelryContext context, IAiUserDirectory users, IAiScopeCatalog scopes, AiToolRegistry registry, IAiTokenStore store)
        {
            _store = store;
            _context = context;
            _users = users;
            _scopes = scopes;
            _registry = registry;
        }

        public async Task<ConnectionListResponse> List(int userId)
        {
            var now = DateTime.UtcNow;
            var rows = await _context.TbtAiToken.AsNoTracking()
                .Where(t => t.UserId == userId && t.RevokedAt == null
                    && ((t.Kind == AiTokenKinds.Pat && t.ExpiresAt > now) || (t.Kind == AiTokenKinds.OAuth && t.RefreshExpiresAt > now)))
                .OrderByDescending(t => t.CreateDate)
                .ToListAsync();

            return new ConnectionListResponse
            {
                Items = rows.Select(t => new ConnectionItem
                {
                    Id = t.Id,
                    Kind = t.Kind,
                    Name = t.Name,
                    ClientId = t.ClientId,
                    Scopes = AiScopes.Split(t.Scopes).ToList(),
                    CreateDate = t.CreateDate,
                    LastUsedAt = t.LastUsedAt,
                    ExpiresAt = t.Kind == AiTokenKinds.Pat ? t.ExpiresAt : t.RefreshExpiresAt ?? t.ExpiresAt,
                }).ToList(),
            };
        }

        public async Task Revoke(int userId, long id)
        {
            var row = await _context.TbtAiToken.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId)
                ?? throw new KeyNotFoundException("ไม่พบการเชื่อมต่อ");
            var now = DateTime.UtcNow;
            if (string.IsNullOrEmpty(row.FamilyId)) await _store.KillRow(row.Id, now);
            else await _store.KillFamily(row.FamilyId, now);
        }

        public async Task<List<PatScopeOption>> PatScopeOptions()
        {
            var scopes = await _scopes.Active();
            return scopes.Where(s => s.Kind == "MODULE" && s.AllowPat)
                .Select(s => new PatScopeOption { Code = s.Code, NameTh = s.NameTh, DescriptionTh = s.DescriptionTh }).ToList();
        }

        public async Task<CreatePatResponse> CreatePat(int userId, CreatePatRequest request)
        {
            var name = request.Name?.Trim();
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("กรุณาตั้งชื่อ token");
            if (name.Length > 100) throw new ArgumentException("ชื่อ token ยาวเกินไป (ไม่เกิน 100 ตัวอักษร)");
            if (!PatDays.Contains(request.ExpiresInDays)) throw new ArgumentException("อายุ token ต้องเป็น 30, 60 หรือ 90 วัน");

            var user = await _users.Load(userId);
            if (user == null || !user.Permissions.Contains(PermissionCodes.AiConnect))
                throw new UnauthorizedAccessException("บัญชีนี้ไม่มีสิทธิ์เชื่อมต่อ AI");
            if (await AiAccessRules.IsSuspended(_context, userId))
                throw new UnauthorizedAccessException(AiErrorMessages.SuspendedTh);

            var activePats = await _context.TbtAiToken.CountAsync(t => t.UserId == userId && t.Kind == AiTokenKinds.Pat && t.RevokedAt == null && t.ExpiresAt > DateTime.UtcNow);
            if (activePats >= MaxActivePatsPerUser)
                throw new ArgumentException($"มี PAT ที่ใช้งานอยู่ครบ {MaxActivePatsPerUser} ตัวแล้ว กรุณายกเลิกตัวที่ไม่ใช้ก่อนสร้างใหม่");

            var requested = (request.Scopes == null || request.Scopes.Count == 0
                ? new List<string> { AiScopes.ModuleExecutive }
                : request.Scopes).Distinct().ToList();

            var catalog = await _scopes.Active();
            foreach (var s in requested)
            {
                var entry = catalog.FirstOrDefault(c => c.Code == s);
                if (entry == null) throw new ArgumentException("ไม่รู้จัก scope " + s);
                if (entry.Kind == "DATA" || s.StartsWith(AiScopes.DataPrefix, StringComparison.Ordinal) || !entry.AllowPat)
                    throw new ArgumentException("PAT ไม่สามารถขอ scope " + s + " ได้ (ข้อมูลอ่อนไหวอนุญาตได้เฉพาะผ่านการเชื่อมต่อแบบ OAuth)");
            }

            var plain = AiTokenKinds.PatPrefix + AiTokenCrypto.RandomToken(32);
            var now = DateTime.UtcNow;
            var row = new TbtAiToken
            {
                Kind = AiTokenKinds.Pat,
                UserId = userId,
                Name = name,
                Scopes = AiScopes.Join(requested),
                TokenHash = AiTokenCrypto.Sha256Hex(plain),
                ExpiresAt = now.AddDays(request.ExpiresInDays),
                CreateDate = now,
            };
            _context.TbtAiToken.Add(row);
            await _context.SaveChangesAsync();

            return new CreatePatResponse { Id = row.Id, Name = name, Token = plain, ExpiresAt = row.ExpiresAt, Scopes = requested };
        }

        public async Task<AccessLogResponse> AccessLog(int userId, AccessLogRequest request)
        {
            var take = Math.Clamp(request.Take, 1, 100);
            var skip = Math.Max(0, request.Skip);

            var q = _context.TbtAiAccessLog.AsNoTracking().Where(l => l.UserId == userId);
            if (request.From.HasValue)
            {
                var from = request.From.Value.UtcDateTime;
                q = q.Where(l => l.CreateDate >= from);
            }
            if (request.To.HasValue)
            {
                var to = request.To.Value.UtcDateTime;
                q = q.Where(l => l.CreateDate <= to);
            }

            var myTokens = _context.TbtAiToken.AsNoTracking().Where(t => t.UserId == userId);

            if (request.TokenIds != null && request.TokenIds.Count > 0)
            {
                var ids = request.TokenIds;
                var families = await myTokens.Where(t => ids.Contains(t.Id) && t.FamilyId != null).Select(t => t.FamilyId!).Distinct().ToListAsync();
                var tokenIds = await myTokens.Where(t => ids.Contains(t.Id) || (t.FamilyId != null && families.Contains(t.FamilyId))).Select(t => t.Id).ToListAsync();
                q = q.Where(l => l.TokenId != null && tokenIds.Contains(l.TokenId.Value));
            }
            if (request.ClientIds != null && request.ClientIds.Count > 0)
            {
                var clients = request.ClientIds;
                var tokenIds = await myTokens.Where(t => t.ClientId != null && clients.Contains(t.ClientId)).Select(t => t.Id).ToListAsync();
                q = q.Where(l => l.TokenId != null && tokenIds.Contains(l.TokenId.Value));
            }

            var total = await q.CountAsync();
            var logs = await q.OrderByDescending(l => l.CreateDate).ThenByDescending(l => l.Id).Skip(skip).Take(take).ToListAsync();

            var logTokenIds = logs.Where(l => l.TokenId != null).Select(l => l.TokenId!.Value).Distinct().ToList();
            var tokens = await myTokens.Where(t => logTokenIds.Contains(t.Id)).Select(t => new { t.Id, t.Name, t.ClientId }).ToListAsync();

            return new AccessLogResponse
            {
                Total = total,
                Data = logs.Select(l =>
                {
                    var tok = tokens.FirstOrDefault(t => t.Id == l.TokenId);
                    return new AccessLogItem
                    {
                        Id = l.Id,
                        CreateDate = l.CreateDate,
                        TokenId = l.TokenId,
                        ConnectionName = tok?.Name,
                        ClientId = tok?.ClientId,
                        Tool = l.Tool,
                        ToolTitleTh = _registry.Find(l.Tool)?.TitleTh ?? l.Tool,
                        Scopes = AiScopes.Split(l.Scopes).ToList(),
                        RowCount = l.RowCount,
                        MaskedCount = l.MaskedCount,
                        DurationMs = l.DurationMs,
                        Success = string.IsNullOrEmpty(l.ErrorCode),
                        ErrorCode = l.ErrorCode,
                        ErrorMessageTh = string.IsNullOrEmpty(l.ErrorCode) ? null : AiErrorMessages.Thai(l.ErrorCode),
                    };
                }).ToList(),
            };
        }
    }
}
