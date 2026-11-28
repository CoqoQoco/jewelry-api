using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Sensitivity;
using Jewelry.Service.Base;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public sealed class AiAdminConflictException : Exception
    {
        public AiAdminConflictException(string message) : base(message) { }
    }

    public interface IAiAdminService
    {
        Task<AiAdminUserListResponse> Users();
        Task<ConnectionListResponse> Connections(int userId);
        Task RevokeConnection(long tokenId);
        Task SetGrant(AiAdminSetGrantRequest request);
        Task Suspend(AiAdminSuspendRequest request);
        Task Lift(AiAdminLiftRequest request);
        Task<DataSourceResult> AccessLog(AiAdminAccessLogRequest request);
        Task<DataSourceResult> Suspensions(AiAdminSuspensionsRequest request);
    }

    public class AiAdminService : BaseService, IAiAdminService
    {
        public const int MaxReasonLength = 500;

        private readonly JewelryContext _context;
        private readonly IAiUserDirectory _users;
        private readonly IAiConnectionService _connections;
        private readonly IAiTokenStore _store;
        private readonly AiToolRegistry _registry;
        private readonly ILogger<AiAdminService>? _logger;

        public AiAdminService(JewelryContext context, IHttpContextAccessor httpContextAccessor, IAiUserDirectory users,
            IAiConnectionService connections, IAiTokenStore store, AiToolRegistry registry, ILogger<AiAdminService>? logger = null) : base(context, httpContextAccessor)
        {
            _logger = logger;
            _context = context;
            _users = users;
            _connections = connections;
            _store = store;
            _registry = registry;
        }

        public async Task<AiAdminUserListResponse> Users()
        {
            var now = DateTime.UtcNow;

            var connectRoleIds = await (from rp in _context.TbtRolePermission
                                        join p in _context.TbmPermission on rp.PermissionId equals p.Id
                                        where p.Code == PermissionCodes.AiConnect && p.IsActive
                                        select rp.RoleId).Distinct().ToListAsync();
            var ids = new HashSet<int>(await _context.TbtUserRole.AsNoTracking()
                .Where(ur => connectRoleIds.Contains(ur.Role)).Select(ur => ur.UserId).Distinct().ToListAsync());

            var tokens = await _context.TbtAiToken.AsNoTracking()
                .Where(t => t.RevokedAt == null
                    && ((t.Kind == AiTokenKinds.Pat && t.ExpiresAt > now) || (t.Kind == AiTokenKinds.OAuth && t.RefreshExpiresAt > now)))
                .Select(t => new { t.UserId, t.LastUsedAt }).ToListAsync();
            var grants = await _context.TbtAiUserGrant.AsNoTracking().Where(g => g.RevokedAt == null).ToListAsync();
            var suspensions = await _context.TbtAiUserSuspension.AsNoTracking().Where(s => s.LiftedAt == null).ToListAsync();

            foreach (var t in tokens) ids.Add(t.UserId);
            foreach (var g in grants) ids.Add(g.UserId);
            foreach (var s in suspensions) ids.Add(s.UserId);

            var items = new List<AiAdminUserItem>();
            foreach (var id in ids.OrderBy(i => i))
            {
                var user = await _users.Load(id);
                if (user == null) continue;
                var mine = tokens.Where(t => t.UserId == id).ToList();
                var susp = suspensions.FirstOrDefault(s => s.UserId == id);
                items.Add(new AiAdminUserItem
                {
                    UserId = id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    Roles = user.Roles,
                    CanConnect = user.Permissions.Contains(PermissionCodes.AiConnect),
                    ActiveConnections = mine.Count,
                    LastUsedAt = mine.Max(t => t.LastUsedAt),
                    Suspended = susp != null,
                    SuspendedReason = susp == null ? null : ReasonText(susp.ReasonDetail),
                    SuspendedAt = susp?.CreatedAt,
                    SuspendedSource = susp?.Source,
                    Grants = SensitiveCategoryPolicy.Rules.Select(r =>
                    {
                        var g = grants.FirstOrDefault(x => x.UserId == id && x.Scope == r.Scope);
                        return new AiAdminGrantState
                        {
                            Scope = r.Scope,
                            NameTh = r.NameTh,
                            Enabled = g != null,
                            CanGrant = user.Permissions.Contains(r.Permission),
                            GrantedBy = g?.GrantedBy,
                            GrantedAt = g?.GrantedAt,
                        };
                    }).ToList(),
                });
            }
            return new AiAdminUserListResponse { Items = items.OrderBy(i => i.Username).ToList() };
        }

        public Task<ConnectionListResponse> Connections(int userId) => _connections.List(userId);

        public async Task RevokeConnection(long tokenId)
        {
            var row = await _context.TbtAiToken.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tokenId)
                ?? throw new KeyNotFoundException("ไม่พบการเชื่อมต่อ");
            var now = DateTime.UtcNow;
            if (string.IsNullOrEmpty(row.FamilyId)) await _store.KillRow(row.Id, now);
            else await _store.KillFamily(row.FamilyId, now);
            _logger?.LogInformation("AI connection revoked by {Actor}: userId={UserId} tokenId={TokenId}", CurrentUsername, row.UserId, row.Id);
        }

        public async Task SetGrant(AiAdminSetGrantRequest request)
        {
            var rule = SensitiveCategoryPolicy.ForScope(request.Scope ?? "")
                ?? throw new ArgumentException("ไม่รู้จักหมวดข้อมูล");
            var user = await _users.Load(request.UserId) ?? throw new KeyNotFoundException("ไม่พบผู้ใช้");
            var now = DateTime.UtcNow;

            if (request.Enabled)
            {
                if (!user.Permissions.Contains(PermissionCodes.AiConnect))
                    throw new ArgumentException("ผู้ใช้นี้ไม่มีสิทธิ์เชื่อมต่อ AI (ai:connect)");
                if (!user.Permissions.Contains(rule.Permission))
                    throw new ArgumentException($"role ของผู้ใช้นี้ไม่มีสิทธิ์ดูข้อมูล \"{rule.NameTh}\" จึงเปิดให้ไม่ได้");

                var exists = await _context.TbtAiUserGrant.AnyAsync(g => g.UserId == request.UserId && g.Scope == rule.Scope && g.RevokedAt == null);
                if (exists) return;
                var grant = new TbtAiUserGrant { UserId = request.UserId, Scope = rule.Scope, GrantedBy = CurrentUsername, GrantedAt = now };
                _context.TbtAiUserGrant.Add(grant);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    _context.Entry(grant).State = EntityState.Detached;
                    // แพ้ race กับอีกคำขอที่เปิดหมวดเดียวกัน — ถือว่าเปิดอยู่แล้ว
                    if (!await _context.TbtAiUserGrant.AnyAsync(g => g.UserId == request.UserId && g.Scope == rule.Scope && g.RevokedAt == null)) throw;
                }
                return;
            }

            var active = await _context.TbtAiUserGrant.Where(g => g.UserId == request.UserId && g.Scope == rule.Scope && g.RevokedAt == null).ToListAsync();
            if (active.Count == 0) return;
            foreach (var g in active)
            {
                g.RevokedBy = CurrentUsername;
                g.RevokedAt = now;
            }
            _context.TbtAiUserGrant.UpdateRange(active);
            await _context.SaveChangesAsync();
        }

        public async Task Suspend(AiAdminSuspendRequest request)
        {
            var reason = request.Reason?.Trim();
            if (string.IsNullOrEmpty(reason)) throw new ArgumentException("กรุณาระบุเหตุผลในการระงับ");
            if (reason.Length > MaxReasonLength) throw new ArgumentException($"เหตุผลยาวเกินไป (ไม่เกิน {MaxReasonLength} ตัวอักษร)");
            if (CurrentUserId == request.UserId.ToString()) throw new ArgumentException("ระงับบัญชีตัวเองไม่ได้");
            if (!await _context.TbtUser.AnyAsync(u => u.Id == request.UserId)) throw new KeyNotFoundException("ไม่พบผู้ใช้");

            const string already = "ผู้ใช้นี้ถูกระงับอยู่แล้ว";
            if (await AiAccessRules.IsSuspended(_context, request.UserId)) throw new AiAdminConflictException(already);

            var row = new TbtAiUserSuspension
            {
                UserId = request.UserId,
                Source = "manual",
                ReasonCode = "manual",
                ReasonDetail = JsonConvert.SerializeObject(new { reason }),
                CreatedBy = CurrentUsername,
                CreatedAt = DateTime.UtcNow,
            };
            _context.TbtAiUserSuspension.Add(row);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // partial unique index (user_id) WHERE lifted_at IS NULL — แพ้ race กับคำขอที่ระงับพร้อมกัน
                _context.Entry(row).State = EntityState.Detached;
                if (await AiAccessRules.IsSuspended(_context, request.UserId)) throw new AiAdminConflictException(already);
                throw;
            }
        }

        public async Task Lift(AiAdminLiftRequest request)
        {
            var row = await _context.TbtAiUserSuspension.FirstOrDefaultAsync(s => s.UserId == request.UserId && s.LiftedAt == null)
                ?? throw new KeyNotFoundException("ผู้ใช้นี้ไม่ได้ถูกระงับ");
            row.LiftedBy = CurrentUsername;
            row.LiftedAt = DateTime.UtcNow;
            _context.TbtAiUserSuspension.Update(row);
            await _context.SaveChangesAsync();
        }

        public async Task<DataSourceResult> AccessLog(AiAdminAccessLogRequest request)
        {
            var q = _context.TbtAiAccessLog.AsNoTracking().AsQueryable();
            var filterUserIds = new List<int>(request.UserIds ?? new List<int>());
            if (request.UserId.HasValue) filterUserIds.Add(request.UserId.Value);
            if (filterUserIds.Count > 0)
            {
                var ids = filterUserIds.Distinct().ToList();
                q = q.Where(l => ids.Contains(l.UserId));
            }
            var filterTools = (request.Tools ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList();
            if (!string.IsNullOrWhiteSpace(request.Tool)) filterTools.Add(request.Tool.Trim());
            if (filterTools.Count > 0)
            {
                var tools = filterTools.Distinct().ToList();
                q = q.Where(l => tools.Contains(l.Tool));
            }
            if (request.DateFrom.HasValue)
            {
                var from = request.DateFrom.Value.UtcDateTime;
                q = q.Where(l => l.CreateDate >= from);
            }
            if (request.DateTo.HasValue)
            {
                var to = request.DateTo.Value.UtcDateTime;
                q = q.Where(l => l.CreateDate <= to);
            }
            q = q.OrderByDescending(l => l.CreateDate).ThenByDescending(l => l.Id);

            request.Take = request.Take <= 0 ? 20 : Math.Min(request.Take, 100);
            var page = q.ToDataSourceResult(request);
            var logs = page.Data.Cast<TbtAiAccessLog>().ToList();

            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var names = await _context.TbtUser.AsNoTracking().Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Username }).ToListAsync();
            var tokenIds = logs.Where(l => l.TokenId != null).Select(l => l.TokenId!.Value).Distinct().ToList();
            var tokens = await _context.TbtAiToken.AsNoTracking().Where(t => tokenIds.Contains(t.Id)).Select(t => new { t.Id, t.Name, t.ClientId }).ToListAsync();
            var clientIds = tokens.Where(t => t.ClientId != null).Select(t => t.ClientId!).Distinct().ToList();
            var clients = await _context.TbtAiClient.AsNoTracking().Where(c => clientIds.Contains(c.ClientId)).Select(c => new { c.ClientId, c.ClientName }).ToListAsync();

            page.Data = logs.Select(l =>
            {
                var tok = tokens.FirstOrDefault(t => t.Id == l.TokenId);
                var client = tok?.ClientId == null ? null : clients.FirstOrDefault(c => c.ClientId == tok.ClientId);
                return new AiAdminAccessLogItem
                {
                    Id = l.Id,
                    CreateDate = l.CreateDate,
                    UserId = l.UserId,
                    Username = names.FirstOrDefault(n => n.Id == l.UserId)?.Username,
                    TokenId = l.TokenId,
                    ClientName = client?.ClientName ?? tok?.Name,
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
            }).ToList();
            return page;
        }

        public async Task<DataSourceResult> Suspensions(AiAdminSuspensionsRequest request)
        {
            var q = _context.TbtAiUserSuspension.AsNoTracking().AsQueryable();
            if (request.UserId.HasValue) q = q.Where(s => s.UserId == request.UserId.Value);
            if (request.Active == true) q = q.Where(s => s.LiftedAt == null);
            else if (request.Active == false) q = q.Where(s => s.LiftedAt != null);
            q = q.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Id);

            request.Take = request.Take <= 0 ? 20 : Math.Min(request.Take, 100);
            var page = q.ToDataSourceResult(request);
            var rows = page.Data.Cast<TbtAiUserSuspension>().ToList();

            var userIds = rows.Select(r => r.UserId).Distinct().ToList();
            var names = await _context.TbtUser.AsNoTracking().Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Username }).ToListAsync();

            page.Data = rows.Select(r => new AiAdminSuspensionItem
            {
                Id = r.Id,
                UserId = r.UserId,
                Username = names.FirstOrDefault(n => n.Id == r.UserId)?.Username,
                Source = r.Source,
                ReasonCode = r.ReasonCode,
                ReasonDetail = r.ReasonDetail,
                ReasonText = ReasonText(r.ReasonDetail),
                CreatedBy = r.CreatedBy,
                CreatedAt = r.CreatedAt,
                LiftedBy = r.LiftedBy,
                LiftedAt = r.LiftedAt,
                Active = r.LiftedAt == null,
            }).ToList();
            return page;
        }

        private static string? ReasonText(string? detail)
        {
            if (string.IsNullOrWhiteSpace(detail)) return null;
            try
            {
                return JObject.Parse(detail).Value<string>("reason") ?? detail;
            }
            catch (JsonException)
            {
                return detail;
            }
        }
    }
}
