using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    // การเขียนที่ต้อง atomic (กัน race ของ code ใช้ครั้งเดียว / refresh หมุน / revoke family) ใช้ conditional UPDATE ฝั่ง DB
    // provider ที่ไม่ใช่ relational (EF InMemory ในเทส) ไม่รองรับ ExecuteUpdate → fallback แบบ tracked (ใช้ในเทสเท่านั้น)
    //
    // family ที่ถูก revoke ทั้งตระกูล (KillFamily) ถูกทำเครื่องหมายด้วย refresh_expires_at <= revoked_at ในแถวที่ถูก revoke
    // ส่วนแถวที่ถูกหมุนตามปกติจะมี revoked_at < refresh_expires_at — ใช้แยกสองกรณีได้โดยไม่ต้องเพิ่มคอลัมน์
    public interface IAiTokenStore
    {
        Task<bool> ClaimAuthCode(string codeHash, DateTime now, CancellationToken ct = default);
        Task<bool> ClaimRefresh(long tokenId, DateTime now, CancellationToken ct = default);
        Task KillFamily(string? familyId, DateTime now, CancellationToken ct = default);
        Task KillRow(long tokenId, DateTime now, CancellationToken ct = default);
        Task<bool> IsFamilyKilled(string familyId, CancellationToken ct = default);
        Task TouchLastUsed(long tokenId, DateTime now, CancellationToken ct = default);
    }

    public class AiTokenStore : IAiTokenStore
    {
        private readonly JewelryContext _context;

        public AiTokenStore(JewelryContext context)
        {
            _context = context;
        }

        private bool Relational => _context.Database.IsRelational();

        public async Task<bool> ClaimAuthCode(string codeHash, DateTime now, CancellationToken ct = default)
        {
            if (Relational)
            {
                var n = await _context.TbtAiAuthCode.Where(c => c.CodeHash == codeHash && c.UsedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.UsedAt, now), ct);
                return n == 1;
            }
            var row = await _context.TbtAiAuthCode.FirstOrDefaultAsync(c => c.CodeHash == codeHash && c.UsedAt == null, ct);
            if (row == null) return false;
            row.UsedAt = now;
            _context.TbtAiAuthCode.Update(row);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ClaimRefresh(long tokenId, DateTime now, CancellationToken ct = default)
        {
            if (Relational)
            {
                var n = await _context.TbtAiToken.Where(t => t.Id == tokenId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
                return n == 1;
            }
            var row = await _context.TbtAiToken.FirstOrDefaultAsync(t => t.Id == tokenId && t.RevokedAt == null, ct);
            if (row == null) return false;
            row.RevokedAt = now;
            _context.TbtAiToken.Update(row);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task KillFamily(string? familyId, DateTime now, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(familyId)) return;
            if (Relational)
            {
                // ปักเครื่องหมาย "ถูก kill" ให้แถวที่ revoke อยู่แล้วก่อน แล้วค่อย revoke แถวที่เหลือ
                // — ถ้า request อื่นเห็นแถวที่เพิ่งถูก revoke จะเห็นเครื่องหมายเสมอ ไม่มีช่วงที่ revoke แล้วแต่ยังไม่มีเครื่องหมาย
                await _context.TbtAiToken.Where(t => t.FamilyId == familyId && t.RevokedAt != null && t.RefreshExpiresAt > t.RevokedAt)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RefreshExpiresAt, t => t.RevokedAt), ct);
                await _context.TbtAiToken.Where(t => t.FamilyId == familyId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.RefreshExpiresAt, (DateTime?)now), ct);
                return;
            }
            var rows = await _context.TbtAiToken.Where(t => t.FamilyId == familyId).ToListAsync(ct);
            foreach (var t in rows)
            {
                t.RevokedAt ??= now;
                if (t.RefreshExpiresAt == null || t.RefreshExpiresAt > t.RevokedAt) t.RefreshExpiresAt = t.RevokedAt;
            }
            _context.TbtAiToken.UpdateRange(rows);
            await _context.SaveChangesAsync(ct);
        }

        public async Task KillRow(long tokenId, DateTime now, CancellationToken ct = default)
        {
            if (Relational)
            {
                await _context.TbtAiToken.Where(t => t.Id == tokenId && t.RevokedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
                return;
            }
            var row = await _context.TbtAiToken.FirstOrDefaultAsync(t => t.Id == tokenId && t.RevokedAt == null, ct);
            if (row == null) return;
            row.RevokedAt = now;
            _context.TbtAiToken.Update(row);
            await _context.SaveChangesAsync(ct);
        }

        public Task<bool> IsFamilyKilled(string familyId, CancellationToken ct = default) =>
            _context.TbtAiToken.AsNoTracking().AnyAsync(t => t.FamilyId == familyId && t.RevokedAt != null
                && t.RefreshExpiresAt != null && t.RefreshExpiresAt <= t.RevokedAt, ct);

        public async Task TouchLastUsed(long tokenId, DateTime now, CancellationToken ct = default)
        {
            if (Relational)
            {
                await _context.TbtAiToken.Where(t => t.Id == tokenId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedAt, (DateTime?)now), ct);
                return;
            }
            var row = await _context.TbtAiToken.FirstOrDefaultAsync(t => t.Id == tokenId, ct);
            if (row == null) return;
            row.LastUsedAt = now;
            _context.TbtAiToken.Update(row);
            await _context.SaveChangesAsync(ct);
        }
    }
}
