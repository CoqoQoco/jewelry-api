using Jewelry.Data.Context;
using Jewelry.Service.Production.Shared;
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

        public async Task<(List<ProductionPlanTrendEvaluator.PlanTrendRow> Plans, List<ProductionPlanFlowCalculator.HeaderRow> Headers, DateTime? MinReceiveDate)> GetTrendDataAsync()
        {
            if (_cache.TryGetValue<(List<ProductionPlanTrendEvaluator.PlanTrendRow>, List<ProductionPlanFlowCalculator.HeaderRow>, DateTime?)>(CacheKey, out var cached))
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
                    CompletedDate = p.CompletedDate,
                    Status = p.Status,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    RequestDate = p.RequestDate,
                    CustomerNumber = p.CustomerNumber,
                    ProductQtyUnit = p.ProductQtyUnit,
                    ProductType = p.ProductType,
                    Type = p.Type,
                    TypeSize = p.TypeSize
                })
                .ToListAsync();

            var headers = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.IsActive == true)
                .Select(h => new ProductionPlanFlowCalculator.HeaderRow
                {
                    ProductionPlanId = h.ProductionPlanId,
                    Status = h.Status,
                    CreateDate = h.CreateDate,
                    ReceiveDate = h.ReceiveDate
                })
                .ToListAsync();

            var backdateClosed = await BackdateClosedPlans.LoadAsync(_jewelryContext);
            if (backdateClosed.Count > 0)
            {
                // effective exit = header ล่าสุด (create/receive) ก่อน header ปิดย้อนหลัง; ไม่มี -> plan.CreateDate
                var lastRealMove = headers
                    .Where(h => backdateClosed.TryGetValue(h.ProductionPlanId, out var markerDate)
                        && !(h.Status == BackdateClosedPlans.StatusCompleted) && h.CreateDate < markerDate)
                    .GroupBy(h => h.ProductionPlanId)
                    .ToDictionary(g => g.Key, g => g.Max(x => x.ReceiveDate.HasValue && x.ReceiveDate.Value > x.CreateDate ? x.ReceiveDate.Value : x.CreateDate));

                foreach (var plan in plans)
                {
                    if (!backdateClosed.TryGetValue(plan.Id, out var markerDate)) continue;
                    var exit = lastRealMove.TryGetValue(plan.Id, out var last) ? last : plan.CreateDate;
                    plan.BackdateClosed = true;
                    plan.CompletedDate = exit < markerDate ? exit : markerDate;
                }

                headers = headers.Where(h => !(h.Status == BackdateClosedPlans.StatusCompleted && backdateClosed.ContainsKey(h.ProductionPlanId))).ToList();
            }

            // MinReceiveDate: คำนวณจาก headers ที่โหลดมาแล้วในหน่วยความจำเลย ไม่ query DB เพิ่ม (ฟรี — ข้อมูลมีอยู่แล้ว)
            var receiveDates = headers.Where(h => h.ReceiveDate.HasValue).Select(h => h.ReceiveDate!.Value).ToList();
            var minReceiveDate = receiveDates.Count > 0 ? receiveDates.Min() : (DateTime?)null;

            var result = (plans, headers, minReceiveDate);
            _cache.Set(CacheKey, result, CacheTtl);
            return result;
        }
    }
}
