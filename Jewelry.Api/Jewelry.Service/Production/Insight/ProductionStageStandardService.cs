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
    // ตาราง tbt_production_stage_standard เป็น append-only history — เก็บทุกแผนกรวมกันไม่กี่สิบแถวตลอดอายุระบบ
    // โหลดทั้งตารางแล้วเลือก "ล่าสุดต่อแผนก" ในหน่วยความจำ (ปลอดภัยกว่า grouped.First() ที่ EF แปลเป็น SQL
    // และตารางเล็กมากไม่ต้องกังวลเรื่อง performance) — cache 5 นาที, invalidate ทันทีหลัง SaveAsync
    public class ProductionStageStandardService : IProductionStageStandardService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string CurrentCacheKey = "ProductionInsight:StageStandard:Current";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionStageStandardService(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<Dictionary<string, StageStandardRow>> GetCurrentStandardsAsync()
        {
            if (_cache.TryGetValue<Dictionary<string, StageStandardRow>>(CurrentCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbtProductionStageStandard
                .AsNoTracking()
                .Select(s => new StageStandardRow
                {
                    DeptKey = s.DeptKey,
                    StandardDays = s.StandardDays,
                    EffectiveFrom = s.EffectiveFrom,
                    CreateBy = s.CreateBy,
                    Remark = s.Remark
                })
                .ToListAsync();

            var result = rows
                .GroupBy(r => r.DeptKey)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.EffectiveFrom).First());

            _cache.Set(CurrentCacheKey, result, CacheTtl);
            return result;
        }

        public async Task<List<StageStandardRow>> GetHistoryAsync(string deptKey)
        {
            return await _jewelryContext.TbtProductionStageStandard
                .AsNoTracking()
                .Where(s => s.DeptKey == deptKey)
                .OrderByDescending(s => s.EffectiveFrom)
                .Select(s => new StageStandardRow
                {
                    DeptKey = s.DeptKey,
                    StandardDays = s.StandardDays,
                    EffectiveFrom = s.EffectiveFrom,
                    CreateBy = s.CreateBy,
                    Remark = s.Remark
                })
                .ToListAsync();
        }

        public async Task SaveAsync(List<(string DeptKey, decimal StandardDays)> items, string? remark, string createBy)
        {
            var now = DateTime.UtcNow;

            var newRows = items.Select(i => new TbtProductionStageStandard
            {
                DeptKey = i.DeptKey,
                StandardDays = i.StandardDays,
                EffectiveFrom = now,
                Remark = remark,
                CreateDate = now,
                CreateBy = createBy
            }).ToList();

            await _jewelryContext.TbtProductionStageStandard.AddRangeAsync(newRows);
            await _jewelryContext.SaveChangesAsync();

            _cache.Remove(CurrentCacheKey);
        }
    }
}
