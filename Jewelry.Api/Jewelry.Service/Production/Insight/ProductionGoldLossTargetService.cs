using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    // ตาราง tbt_production_gold_loss_target เป็น append-only history (SLIP×2worker_type×2metal + STAGE×3×2 รวม
    // ไม่กี่สิบแถวตลอดอายุระบบ) — โหลดทั้งตารางแล้วเลือก "ล่าสุดต่อ (scope, worker_type, metal)" ในหน่วยความจำ
    // (ห้าม grouped.First() แปลเป็น SQL) — cache 5 นาที, invalidate ทันทีหลัง SaveAsync
    public class ProductionGoldLossTargetService : IProductionGoldLossTargetService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string CurrentCacheKey = "ProductionInsight:GoldLossTarget:Current";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionGoldLossTargetService(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<Dictionary<(string Scope, int WorkerType, string Metal), GoldLossTargetRow>> GetCurrentTargetsAsync()
        {
            if (_cache.TryGetValue<Dictionary<(string, int, string), GoldLossTargetRow>>(CurrentCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbtProductionGoldLossTarget
                .AsNoTracking()
                .Select(t => new GoldLossTargetRow
                {
                    Scope = t.Scope,
                    WorkerType = t.WorkerType,
                    Metal = t.Metal,
                    TargetPercent = t.TargetPercent,
                    EffectiveFrom = t.EffectiveFrom,
                    CreateBy = t.CreateBy,
                    Remark = t.Remark
                })
                .ToListAsync();

            var result = rows
                .GroupBy(r => (r.Scope, r.WorkerType, r.Metal))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EffectiveFrom).First());

            _cache.Set(CurrentCacheKey, result, CacheTtl);
            return result;
        }

        public async Task<List<GoldLossTargetRow>> GetHistoryAsync(string scope, int workerType, string metal)
        {
            return await _jewelryContext.TbtProductionGoldLossTarget
                .AsNoTracking()
                .Where(t => t.Scope == scope && t.WorkerType == workerType && t.Metal == metal)
                .OrderByDescending(t => t.EffectiveFrom)
                .Select(t => new GoldLossTargetRow
                {
                    Scope = t.Scope,
                    WorkerType = t.WorkerType,
                    Metal = t.Metal,
                    TargetPercent = t.TargetPercent,
                    EffectiveFrom = t.EffectiveFrom,
                    CreateBy = t.CreateBy,
                    Remark = t.Remark
                })
                .ToListAsync();
        }

        public async Task SaveAsync(List<(string Scope, int WorkerType, string Metal, decimal TargetPercent)> items, string? remark, string createBy)
        {
            var now = DateTime.UtcNow;

            var newRows = items.Select(i => new TbtProductionGoldLossTarget
            {
                Scope = i.Scope,
                WorkerType = i.WorkerType,
                Metal = i.Metal,
                TargetPercent = i.TargetPercent,
                EffectiveFrom = now,
                Remark = remark,
                CreateDate = now,
                CreateBy = createBy
            }).ToList();

            await _jewelryContext.TbtProductionGoldLossTarget.AddRangeAsync(newRows);
            await _jewelryContext.SaveChangesAsync();

            _cache.Remove(CurrentCacheKey);
        }
    }
}
