using Jewelry.Data.Context;
using Jewelry.Service.Common;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Overview = jewelry.Model.Stock.Insight.Overview;

namespace Jewelry.Service.Stock.Insight
{
    public class StockInsightService : BaseService, IStockInsightService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly InsightCache _insightCache;

        private const string InStockStatus = "IN_STOCK";

        private static readonly string[] QtyMovementTypes = { "RECEIPT", "RETURN", "SALE", "TRANSFORM_IN", "TRANSFORM_OUT" };

        public StockInsightService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor, InsightCache insightCache)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _insightCache = insightCache;
        }

        public const string CacheEndpoint = "StockInsight:Overview";

        public Task<Overview.Response> Overview(Overview.Request request)
        {
            var key = InsightCache.Key(CacheEndpoint, MetalClassifier.Normalize(request.Metal), StockInsightThresholds.ClampMonths(request.Months));
            return _insightCache.GetOrCreateAsync(key, InsightCache.OverviewTtl, request.Thresholds, () => OverviewCore(request));
        }

        private async Task<Overview.Response> OverviewCore(Overview.Request request)
        {
            var metal = MetalClassifier.Normalize(request.Metal);
            var months = StockInsightThresholds.ClampMonths(request.Months);
            var thresholds = StockInsightThresholds.Resolve(request.Thresholds);
            var now = DateTime.UtcNow;

            var pieces = await GetOnHandPiecesAsync(metal);
            var inStock = pieces.Where(x => x.Status == InStockStatus).ToList();
            var reservedPcs = pieces.Where(x => x.Status != InStockStatus).Sum(x => x.Pcs);
            var onHandQty = (decimal)pieces.Sum(x => x.Pcs);

            // คำนวณ series ยาวพอสำหรับหน้าต่างเฉลี่ย/no-outflow แม้ผู้ใช้ขอ months สั้น แล้วค่อยตัดส่งเฉพาะ months ล่าสุด
            var calcMonths = Math.Max(months, Math.Max(thresholds.CoverWindowMonths + 1, thresholds.NoOutflowMonths));
            var monthStarts = StockInsightCalculator.MonthStarts(now, calcMonths);
            var movements = await GetMovementsAsync(metal, StockInsightCalculator.WindowStartUtc(monthStarts[0]));
            var fullSeries = StockInsightCalculator.BuildSeries(monthStarts, movements, onHandQty);
            var series = fullSeries.Skip(fullSeries.Count - months).ToList();

            var salesStart = await GetSalesStartMonthAsync(metal);
            var historyMonths = StockInsightCalculator.FullMonthsSince(salesStart, monthStarts[monthStarts.Count - 1]);

            var kpi = BuildKpi(inStock, onHandQty, reservedPcs, now);
            var ageBuckets = BuildAgeBuckets(inStock, now);
            var (avgOutflow, windowUsed) = StockInsightCalculator.AverageMonthlyOutflow(fullSeries, thresholds.CoverWindowMonths, historyMonths);

            var problems = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                StockInsightRuleEngine.EvaluateNoCostHigh(kpi.NoCostCount, kpi.InStockCount, thresholds),
                StockInsightRuleEngine.EvaluateAgedOver5y(kpi.AgedOver5yCount, kpi.InStockCount, thresholds),
                StockInsightRuleEngine.EvaluateNoProductionDate(kpi.NoProductionDateCount),
                StockInsightRuleEngine.EvaluateNoOutflow(
                    StockInsightCalculator.HasNoOutflow(fullSeries, thresholds.NoOutflowMonths), kpi.InStockCount, historyMonths, thresholds)
            }.Where(f => f != null).Select(f => f!).ToList();

            var forecasts = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                StockInsightRuleEngine.EvaluateCoverMonths(onHandQty, avgOutflow, historyMonths, windowUsed, thresholds),
                BuildAgingForecast(inStock, now, avgOutflow, historyMonths, windowUsed, thresholds)
            }.Where(f => f != null).Select(f => f!).ToList();

            return new Overview.Response
            {
                AsOf = now,
                Metal = metal,
                Status = StockInsightRuleEngine.WorstSeverity(problems.Concat(forecasts)),
                Thresholds = thresholds,
                SalesHistory = new Overview.SalesHistory
                {
                    StartMonth = salesStart?.ToString("yyyy-MM-dd"),
                    FullMonths = historyMonths
                },
                Kpi = kpi,
                AgeBuckets = ageBuckets,
                ReceiptTypes = BuildReceiptTypes(inStock),
                Categories = BuildCategories(inStock, now),
                Series = series,
                Problems = problems,
                Forecasts = forecasts,
                Actions = StockInsightRuleEngine.BuildActions(problems, forecasts)
            };
        }

        // ชิ้นที่ยังมีของ (IN_STOCK + RESERVED) จาก StockUnitSnapshot — หน่วยนับคือชิ้น (unit) ไม่ใช่แถวล็อต
        private async Task<List<StockUnitBucket>> GetOnHandPiecesAsync(string metal)
        {
            var rows = await StockUnitSnapshot.LoadAsync(_jewelryContext);

            return metal == MetalClassifier.All
                ? rows
                : rows.Where(x => MetalClassifier.Matches(metal, x.Lot.ProductionType)).ToList();
        }

        private async Task<List<StockMovementInput>> GetMovementsAsync(string metal, DateTime fromUtc)
        {
            var rows = await _jewelryContext.TbtStockMovement
                .AsNoTracking()
                .Where(m => m.MovementDate >= fromUtc && QtyMovementTypes.Contains(m.MovementType))
                .Select(m => new
                {
                    m.MovementDate,
                    m.MovementType,
                    m.RefDocType,
                    m.Qty,
                    ProductionType = m.SkuCodeNavigation.ProductionType
                })
                .ToListAsync();

            return rows
                .Where(x => metal == MetalClassifier.All || MetalClassifier.Matches(metal, x.ProductionType))
                .Select(x => new StockMovementInput(x.MovementDate, x.MovementType, x.RefDocType, x.Qty))
                .ToList();
        }

        // เดือนแรก (เวลาไทย) ที่มี SALE ที่ออกจากใบแจ้งหนี้จริง (RefDocType INVOICE) — LEGACY_SALE ไม่นับ
        private async Task<DateTime?> GetSalesStartMonthAsync(string metal)
        {
            var rows = await _jewelryContext.TbtStockMovement
                .AsNoTracking()
                .Where(m => m.MovementType == "SALE" && m.RefDocType == StockInsightCalculator.InvoiceRefDocType)
                .Select(m => new { m.MovementDate, ProductionType = m.SkuCodeNavigation.ProductionType })
                .ToListAsync();

            var dates = rows
                .Where(x => metal == MetalClassifier.All || MetalClassifier.Matches(metal, x.ProductionType))
                .Select(x => x.MovementDate)
                .ToList();

            return dates.Count == 0 ? null : StockInsightCalculator.ThaiMonthStart(dates.Min());
        }

        private static bool IsNoCost(StockUnitBucket x) => (x.Lot.ProductCost ?? 0) == 0;

        // ต้นทุนต่อชิ้น x จำนวนชิ้น (product_cost เป็นราคาต่อชิ้น)
        private static decimal CostOf(StockUnitBucket x) => (x.Lot.ProductCost ?? 0) * x.Pcs;

        private static int LotCount(IEnumerable<StockUnitBucket> rows)
            => rows.Select(x => (x.Lot.StockNumber, x.Lot.ProductCode)).Distinct().Count();

        private static Overview.Kpi BuildKpi(List<StockUnitBucket> inStock, decimal onHandQty, int reservedPcs, DateTime now)
        {
            var count = inStock.Sum(x => x.Pcs);
            var noCostRows = inStock.Where(IsNoCost).ToList();
            var noCost = noCostRows.Sum(x => x.Pcs);
            var over1y = inStock.Where(x => StockInsightCalculator.AgeBucketKey(x.AgeDate, now) != "lt1y").Sum(x => x.Pcs);
            var over5y = inStock.Where(x => StockInsightCalculator.AgeBucketKey(x.AgeDate, now) == "gt5y").Sum(x => x.Pcs);
            var cost = inStock.Sum(CostOf);

            return new Overview.Kpi
            {
                InStockCount = count,
                InStockQty = count,
                InStockLotCount = LotCount(inStock),
                ReservedCount = reservedPcs,
                OnHandQty = onHandQty,
                NoCostCount = noCost,
                NoCostLotCount = LotCount(noCostRows),
                NoCostPercent = StockInsightCalculator.Percent(noCost, count),
                CostThb = cost,
                CostThbQty = cost,
                AgedOver1yCount = over1y,
                AgedOver1yPercent = StockInsightCalculator.Percent(over1y, count),
                AgedOver5yCount = over5y,
                AgedOver5yPercent = StockInsightCalculator.Percent(over5y, count),
                NoProductionDateCount = inStock.Where(x => !x.AgeDate.HasValue).Sum(x => x.Pcs)
            };
        }

        private static List<Overview.AgeBucket> BuildAgeBuckets(List<StockUnitBucket> inStock, DateTime now)
        {
            var keyed = inStock.ToLookup(x => StockInsightCalculator.AgeBucketKey(x.AgeDate, now));
            return new[] { "lt1y", "y1to2", "y2to5", "gt5y" }.Select(key =>
            {
                var list = keyed[key].ToList();
                var cost = list.Sum(CostOf);
                var pcs = list.Sum(x => x.Pcs);
                return new Overview.AgeBucket
                {
                    Key = key,
                    Count = pcs,
                    NoCostCount = list.Where(IsNoCost).Sum(x => x.Pcs),
                    CostThb = cost,
                    Qty = pcs,
                    CostThbQty = cost
                };
            }).ToList();
        }

        private static List<Overview.ReceiptTypeRow> BuildReceiptTypes(List<StockUnitBucket> inStock)
        {
            return inStock
                .GroupBy(x => x.Lot.ReceiptType)
                .Select(g =>
                {
                    var pcs = g.Sum(x => x.Pcs);
                    var cost = g.Sum(CostOf);
                    return new Overview.ReceiptTypeRow
                    {
                        ReceiptType = g.Key,
                        Count = pcs,
                        NoCostCount = g.Where(IsNoCost).Sum(x => x.Pcs),
                        Qty = pcs,
                        CostThb = cost,
                        CostThbQty = cost
                    };
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        // หมวดสินค้า = tbt_sku.product_type (รหัส) แสดงชื่อด้วย product_type_name
        private static List<Overview.CategoryRow> BuildCategories(List<StockUnitBucket> inStock, DateTime now)
        {
            return inStock
                .GroupBy(x => x.Lot.ProductType ?? string.Empty)
                .Select(g =>
                {
                    var pcs = g.Sum(x => x.Pcs);
                    var cost = g.Sum(CostOf);
                    return new Overview.CategoryRow
                    {
                        Category = g.Key,
                        CategoryName = g.Select(x => x.Lot.ProductTypeName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                        Count = pcs,
                        Qty = pcs,
                        CostThb = cost,
                        CostThbQty = cost,
                        NoCostCount = g.Where(IsNoCost).Sum(x => x.Pcs),
                        AgedOver1yCount = g.Where(x => StockInsightCalculator.AgeBucketKey(x.AgeDate, now) != "lt1y").Sum(x => x.Pcs)
                    };
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        // ฐานคาดการณ์ = สต็อก IN_STOCK ถ่วงจำนวนชิ้น; อายุเกิน 1 ปี = ไม่ใช่ lt1y (ไม่มีวันเริ่มนับอายุ = เก่า)
        private static jewelry.Model.Production.Insight.Wip.Finding? BuildAgingForecast(
            List<StockUnitBucket> inStock, DateTime now, decimal avgOutflow, int historyMonths, int windowMonths, jewelry.Model.Stock.Insight.Overview.ThresholdValues t)
        {
            var horizon = t.AgingProjectionMonths;
            var total = (decimal)inStock.Sum(x => x.Pcs);
            if (total <= 0) return null;

            var aged = 0m;
            var turning = new decimal[horizon];
            foreach (var x in inStock)
            {
                if (StockInsightCalculator.AgeBucketKey(x.AgeDate, now) != "lt1y") { aged += x.Pcs; continue; }

                var k = StockInsightCalculator.TurningMonthIndex(x.AgeDate!.Value, now, horizon);
                if (k.HasValue) turning[k.Value - 1] += x.Pcs;
            }

            var from = aged * 100m / total;
            var to = StockInsightCalculator.ProjectAgedPercent(total, aged, turning, avgOutflow, horizon);
            return StockInsightRuleEngine.EvaluateAgingProjected(from, to, historyMonths, windowMonths, t);
        }
    }
}
