using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    // กติกากลางของสิทธิ์ข้อมูล AI รายคน (grant) + การระงับ (suspension)
    public static class AiAccessRules
    {
        public static Task<bool> IsSuspended(JewelryContext context, int userId, CancellationToken ct = default) =>
            context.TbtAiUserSuspension.AsNoTracking().AnyAsync(s => s.UserId == userId && s.LiftedAt == null, ct);

        // หมวด DATA ที่ Dev เปิดให้ user (เฉพาะ scope ที่ระบบรู้จัก)
        public static async Task<List<string>> ActiveGrants(JewelryContext context, int userId, CancellationToken ct = default)
        {
            var scopes = await context.TbtAiUserGrant.AsNoTracking()
                .Where(g => g.UserId == userId && g.RevokedAt == null)
                .Select(g => g.Scope).ToListAsync(ct);
            return scopes.Distinct().Where(s => Sensitivity.SensitiveCategoryPolicy.ForScope(s) != null).ToList();
        }

        public static bool IsDataScope(string scope) => scope.StartsWith(AiScopes.DataPrefix, System.StringComparison.Ordinal);

        // MODULE จาก token เสมอ · DATA จาก grant เฉพาะ token OAUTH (PAT ไม่ได้ DATA) · DATA ที่ฝังใน token ไม่ถูกใช้
        public static async Task<List<string>> EffectiveScopes(JewelryContext context, int userId, string? tokenKind, IEnumerable<string> tokenScopes, CancellationToken ct = default)
        {
            var result = tokenScopes.Where(s => !IsDataScope(s)).ToList();
            if (tokenKind == AiTokenKinds.OAuth)
                result.AddRange(await ActiveGrants(context, userId, ct));
            return result;
        }

        public static string StripData(string? scopes) =>
            AiScopes.Join(AiScopes.Split(scopes).Where(s => !IsDataScope(s)));
    }
}
