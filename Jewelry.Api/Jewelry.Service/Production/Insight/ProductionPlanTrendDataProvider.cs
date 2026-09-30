using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    // โหลดข้อมูลดิบ (plan + active status header ทั้งหมด) สำหรับ WipTrend/WIP_DEPT_GROWING ครั้งเดียว
    // cache 5 นาที — ไม่ผูกกับ start/end/bucket เพราะ evaluator คำนวณ WIP/flow ย้อนเวลาเองในหน่วยความจำ
    public class ProductionPlanTrendDataProvider : IProductionPlanTrendDataProvider
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string CacheKey = "ProductionInsight:TrendData";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionPlanTrendDataProvider(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<(List<ProductionPlanTrendEvaluator.PlanTrendRow> Plans, List<ProductionPlanFlowCalculator.HeaderRow> Headers)> GetTrendDataAsync()
        {
            if (_cache.TryGetValue<(List<ProductionPlanTrendEvaluator.PlanTrendRow>, List<ProductionPlanFlowCalculator.HeaderRow>)>(CacheKey, out var cached))
            {
                return cached;
            }

            var plans = await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true)
                .Select(p => new ProductionPlanTrendEvaluator.PlanTrendRow
                {
                    Id = p.Id,
                    CreateDate = p.CreateDate,
                    CompletedDate = p.CompletedDate
                })
                .ToListAsync();

            var headers = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.IsActive == true)
                .Select(h => new ProductionPlanFlowCalculator.HeaderRow
                {
                    ProductionPlanId = h.ProductionPlanId,
                    Status = h.Status,
                    CreateDate = h.CreateDate
                })
                .ToListAsync();

            var result = (plans, headers);
            _cache.Set(CacheKey, result, CacheTtl);
            return result;
        }
    }
}
