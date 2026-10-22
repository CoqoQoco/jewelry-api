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

        private class PieceRow
        {
            public string? Status { get; set; }
            public decimal Qty { get; set; }
            public decimal? ProductCost { get; set; }
            public DateTime? ProductionDate { get; set; }
            public string? ReceiptType { get; set; }
            public string? ProductionType { get; set; }
            public string? ProductType { get; set; }
            public string? ProductTypeName { get; set; }
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
            var onHandQty = pieces.Sum(x => x.Qty);

            // คำนวณ series ยาวพอสำหรับหน้าต่างเฉลี่ย/no-outflow แม้ผู้ใช้ขอ months สั้น แล้วค่อยตัดส่งเฉพาะ months ล่าสุด
            var calcMonths = Math.Max(months, Math.Max(thresholds.CoverWindowMonths + 1, thresholds.NoOutflowMonths));
            var monthStarts = StockInsightCalculator.MonthStarts(now, calcMonths);
            var movements = await GetMovementsAsync(metal, StockInsightCalculator.WindowStartUtc(monthStarts[0]));
            var fullSeries = StockInsightCalculator.BuildSeries(monthStarts, movements, onHandQty);
            var series = fullSeries.Skip(fullSeries.Count - months).ToList();

            var salesStart = await GetSalesStartMonthAsync(metal);
            var historyMonths = StockInsightCalculator.FullMonthsSince(salesStart, monthStarts[monthStarts.Count - 1]);

            var kpi = BuildKpi(inStock, onHandQty, now);
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

        // piece ที่ qty > 0 (IN_STOCK + RESERVED) — flat projection แล้ว aggregate ใน memory (~30k แถว)
        private async Task<List<PieceRow>> GetOnHandPiecesAsync(string metal)
        {
            var rows = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.Qty > 0)
                .Select(x => new PieceRow
                {
                    Status = x.Status,
                    Qty = x.Qty,
                    ProductCost = x.ProductCost,
                    ProductionDate = x.ProductionDate,
                    ReceiptType = x.ReceiptType,
                    ProductionType = x.SkuCodeNavigation.ProductionType,
                    ProductType = x.SkuCodeNavigation.ProductType,
                    ProductTypeName = x.SkuCodeNavigation.ProductTypeName
                })
                .ToListAsync();

            return metal == MetalClassifier.All
                ? rows
                : rows.Where(x => MetalClassifier.Matches(metal, x.ProductionType)).ToList();
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

        private static bool IsNoCost(PieceRow x) => (x.ProductCost ?? 0) == 0;

        private static Overview.Kpi BuildKpi(List<PieceRow> inStock, decimal onHandQty, DateTime now)
        {
            var count = inStock.Count;
            var noCost = inStock.Count(IsNoCost);
            var over1y = inStock.Count(x => StockInsightCalculator.AgeBucketKey(x.ProductionDate, now) != "lt1y");
            var over5y = inStock.Count(x => StockInsightCalculator.AgeBucketKey(x.ProductionDate, now) == "gt5y");

            return new Overview.Kpi
            {
                InStockCount = count,
                InStockQty = inStock.Sum(x => x.Qty),
                OnHandQty = onHandQty,
                NoCostCount = noCost,
                NoCostPercent = StockInsightCalculator.Percent(noCost, count),
                CostThb = inStock.Sum(x => x.ProductCost ?? 0),
                CostThbQty = inStock.Sum(x => (x.ProductCost ?? 0) * x.Qty),
                AgedOver1yCount = over1y,
                AgedOver1yPercent = StockInsightCalculator.Percent(over1y, count),
                AgedOver5yCount = over5y,
                AgedOver5yPercent = StockInsightCalculator.Percent(over5y, count),
                NoProductionDateCount = inStock.Count(x => !x.ProductionDate.HasValue)
            };
        }

        private static List<Overview.AgeBucket> BuildAgeBuckets(List<PieceRow> inStock, DateTime now)
        {
            var keyed = inStock.ToLookup(x => StockInsightCalculator.AgeBucketKey(x.ProductionDate, now));
            return new[] { "lt1y", "y1to2", "y2to5", "gt5y" }.Select(key =>
            {
                var list = keyed[key].ToList();
                return new Overview.AgeBucket
                {
                    Key = key,
                    Count = list.Count,
                    NoCostCount = list.Count(IsNoCost),
                    CostThb = list.Sum(x => x.ProductCost ?? 0),
                    Qty = list.Sum(x => x.Qty),
                    CostThbQty = list.Sum(x => (x.ProductCost ?? 0) * x.Qty)
                };
            }).ToList();
        }

        private static List<Overview.ReceiptTypeRow> BuildReceiptTypes(List<PieceRow> inStock)
        {
            return inStock
                .GroupBy(x => x.ReceiptType)
                .Select(g => new Overview.ReceiptTypeRow
                {
                    ReceiptType = g.Key,
                    Count = g.Count(),
                    NoCostCount = g.Count(IsNoCost),
                    Qty = g.Sum(x => x.Qty),
                    CostThb = g.Sum(x => x.ProductCost ?? 0)
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        // หมวดสินค้า = tbt_sku.product_type (รหัส) แสดงชื่อด้วย product_type_name
        private static List<Overview.CategoryRow> BuildCategories(List<PieceRow> inStock, DateTime now)
        {
            return inStock
                .GroupBy(x => x.ProductType ?? string.Empty)
                .Select(g => new Overview.CategoryRow
                {
                    Category = g.Key,
                    CategoryName = g.Select(x => x.ProductTypeName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                    Count = g.Count(),
                    Qty = g.Sum(x => x.Qty),
                    CostThb = g.Sum(x => x.ProductCost ?? 0),
                    NoCostCount = g.Count(IsNoCost),
                    AgedOver1yCount = g.Count(x => StockInsightCalculator.AgeBucketKey(x.ProductionDate, now) != "lt1y")
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        // ฐานคาดการณ์ = สต็อก IN_STOCK ถ่วง qty; อายุเกิน 1 ปี = ไม่ใช่ lt1y (production_date ว่าง = เก่า)
        private static jewelry.Model.Production.Insight.Wip.Finding? BuildAgingForecast(
            List<PieceRow> inStock, DateTime now, decimal avgOutflow, int historyMonths, int windowMonths, jewelry.Model.Stock.Insight.Overview.ThresholdValues t)
        {
            var horizon = t.AgingProjectionMonths;
            var total = inStock.Sum(x => x.Qty);
            if (total <= 0) return null;

            var aged = 0m;
            var turning = new decimal[horizon];
            foreach (var x in inStock)
            {
                if (StockInsightCalculator.AgeBucketKey(x.ProductionDate, now) != "lt1y") { aged += x.Qty; continue; }

                var k = StockInsightCalculator.TurningMonthIndex(x.ProductionDate!.Value, now, horizon);
                if (k.HasValue) turning[k.Value - 1] += x.Qty;
            }

            var from = aged * 100m / total;
            var to = StockInsightCalculator.ProjectAgedPercent(total, aged, turning, avgOutflow, horizon);
            return StockInsightRuleEngine.EvaluateAgingProjected(from, to, historyMonths, windowMonths, t);
        }
    }
}
