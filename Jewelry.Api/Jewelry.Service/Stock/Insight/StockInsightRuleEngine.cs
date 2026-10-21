using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Stock.Insight.Overview;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Stock.Insight
{
    // Pure — ไม่แตะ DB, รับตัวเลขที่คำนวณแล้วเป็น argument (unit-testable); ใช้ Finding/ActionItem shape เดียวกับ ProductionInsight
    public static class StockInsightRuleEngine
    {
        private const string Critical = "critical";
        private const string Warning = "warning";

        private static string? SeverityByPercent(decimal percent, decimal warn, decimal critical)
        {
            if (percent >= critical) return Critical;
            if (percent >= warn) return Warning;
            return null;
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        // ---- problems ----

        public static Wip.Finding? EvaluateNoCostHigh(int noCostCount, int inStockCount, ThresholdValues t)
        {
            if (inStockCount <= 0 || noCostCount <= 0) return null;

            var percent = (decimal)noCostCount * 100m / inStockCount;
            var severity = SeverityByPercent(percent, t.NoCostWarnPercent, t.NoCostCriticalPercent);
            if (severity == null) return null;

            return new Wip.Finding
            {
                Code = "STK_NO_COST_HIGH",
                Severity = severity,
                ReportRef = "stkCost",
                Params = new Dictionary<string, object>
                {
                    ["noCostCount"] = noCostCount,
                    ["inStockCount"] = inStockCount,
                    ["percent"] = Round2(percent)
                }
            };
        }

        public static Wip.Finding? EvaluateAgedOver5y(int count, int inStockCount, ThresholdValues t)
        {
            if (inStockCount <= 0 || count <= 0) return null;

            var percent = (decimal)count * 100m / inStockCount;
            var severity = SeverityByPercent(percent, t.Aged5yWarnPercent, t.Aged5yCriticalPercent);
            if (severity == null) return null;

            return new Wip.Finding
            {
                Code = "STK_AGED_OVER_5Y",
                Severity = severity,
                ReportRef = "stkAge",
                Params = new Dictionary<string, object> { ["count"] = count, ["percent"] = Round2(percent) }
            };
        }

        public static Wip.Finding? EvaluateNoProductionDate(int count)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "STK_NO_PRODUCTION_DATE",
                Severity = Warning,
                ReportRef = "stkAge",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        public static Wip.Finding? EvaluateNoOutflow(bool noOutflow, int inStockCount, int historyMonths, ThresholdValues t)
        {
            if (!noOutflow || inStockCount <= 0 || historyMonths < t.MinHistoryMonths) return null;

            return new Wip.Finding
            {
                Code = "STK_NO_OUTFLOW",
                Severity = Warning,
                ReportRef = "stkTrend",
                Params = new Dictionary<string, object> { ["months"] = t.NoOutflowMonths, ["historyMonths"] = historyMonths }
            };
        }

        // ---- forecasts ----

        // coverMonths = endStockQty / avgMonthlyOutflow; avgMonthlyOutflow = 0 และมีของ = ล้น (high, coverMonths = null)
        // low: warning, ต่ำกว่าครึ่งหนึ่งของ low = critical; high: warning
        public static Wip.Finding? EvaluateCoverMonths(decimal endStockQty, decimal avgMonthlyOutflow, int historyMonths, int windowMonths, ThresholdValues t)
        {
            if (endStockQty <= 0 || historyMonths < t.MinHistoryMonths) return null;

            decimal? cover = avgMonthlyOutflow > 0 ? endStockQty / avgMonthlyOutflow : null;

            string direction;
            string severity;
            if (cover == null)
            {
                direction = "high";
                severity = Warning;
            }
            else if (cover.Value < t.CoverMonthsLow)
            {
                direction = "low";
                severity = cover.Value < t.CoverMonthsLow / 2m ? Critical : Warning;
            }
            else if (cover.Value > t.CoverMonthsHigh)
            {
                direction = "high";
                severity = Warning;
            }
            else
            {
                return null;
            }

            return new Wip.Finding
            {
                Code = "FC_STK_COVER_MONTHS",
                Severity = severity,
                ReportRef = "stkTrend",
                Params = new Dictionary<string, object>
                {
                    ["coverMonths"] = cover.HasValue ? Math.Round(cover.Value, 1, MidpointRounding.AwayFromZero) : null!,
                    ["avgMonthlyOutflow"] = Math.Round(avgMonthlyOutflow, 1, MidpointRounding.AwayFromZero),
                    ["endStockQty"] = endStockQty,
                    ["direction"] = direction,
                    ["historyMonths"] = historyMonths,
                    ["windowMonths"] = windowMonths
                }
            };
        }

        public static Wip.Finding? EvaluateAgingProjected(decimal fromPercent, decimal toPercent, int historyMonths, int windowMonths, ThresholdValues t)
        {
            if (historyMonths < t.MinHistoryMonths) return null;
            if (toPercent - fromPercent < t.AgingRisePointsWarn || toPercent <= fromPercent) return null;

            return new Wip.Finding
            {
                Code = "FC_STK_AGING_PROJECTED",
                Severity = Warning,
                ReportRef = "stkAge",
                Params = new Dictionary<string, object>
                {
                    ["fromPercent"] = Round2(fromPercent),
                    ["toPercent"] = Round2(toPercent),
                    ["months"] = t.AgingProjectionMonths,
                    ["historyMonths"] = historyMonths,
                    ["windowMonths"] = windowMonths
                }
            };
        }

        // 'critical' | 'warning' | 'ok'
        public static string WorstSeverity(IEnumerable<Wip.Finding> findings)
        {
            var list = findings.ToList();
            if (list.Any(f => f.Severity == Critical)) return "critical";
            if (list.Any()) return "warning";
            return "ok";
        }

        // ---- actions ----

        private static readonly (string Code, string OwnerRole, string[] RelatedCodes)[] ActionDefinitions =
        {
            ("ACT_PRICE_NO_COST", "productionManager", new[] { "STK_NO_COST_HIGH" }),
            ("ACT_CLEAR_AGED", "productionManager", new[] { "STK_AGED_OVER_5Y", "FC_STK_AGING_PROJECTED" }),
            ("ACT_REPLENISH", "planner", new[] { "FC_STK_COVER_MONTHS" }),
            ("ACT_SLOW_INTAKE", "planner", new[] { "FC_STK_COVER_MONTHS" }),
            ("ACT_FIX_PRODUCTION_DATE", "deptHead", new[] { "STK_NO_PRODUCTION_DATE" })
        };

        // priority: critical ก่อน warning แล้วตามลำดับที่ประกาศ; REPLENISH/SLOW_INTAKE แยกด้วย params.direction ของ FC_STK_COVER_MONTHS
        public static List<Wip.ActionItem> BuildActions(IReadOnlyList<Wip.Finding> problems, IReadOnlyList<Wip.Finding> forecasts)
        {
            var byCode = problems.Concat(forecasts).ToLookup(f => f.Code);
            var built = new List<(Wip.ActionItem Action, int Rank, int Index)>();

            for (var i = 0; i < ActionDefinitions.Length; i++)
            {
                var def = ActionDefinitions[i];
                var related = def.RelatedCodes.SelectMany(c => byCode[c]).ToList();

                if (def.Code == "ACT_REPLENISH") related = related.Where(f => DirectionOf(f) == "low").ToList();
                if (def.Code == "ACT_SLOW_INTAKE") related = related.Where(f => DirectionOf(f) == "high").ToList();
                if (related.Count == 0) continue;

                var action = new Wip.ActionItem
                {
                    Code = def.Code,
                    OwnerRole = def.OwnerRole,
                    RelatedCodes = def.RelatedCodes.ToList(),
                    Params = BuildActionParams(def.Code, byCode)
                };
                built.Add((action, related.Min(f => Rank(f.Severity)), i));
            }

            var ordered = built.OrderBy(x => x.Rank).ThenBy(x => x.Index).ToList();
            for (var i = 0; i < ordered.Count; i++) ordered[i].Action.Priority = i + 1;
            return ordered.Select(x => x.Action).ToList();
        }

        private static int Rank(string severity) => severity == Critical ? 0 : severity == Warning ? 1 : 2;

        private static string? DirectionOf(Wip.Finding f)
            => f.Params.TryGetValue("direction", out var d) ? d as string : null;

        private static object? Param(ILookup<string, Wip.Finding> findings, string code, string key)
        {
            var f = findings[code].FirstOrDefault();
            return f != null && f.Params.TryGetValue(key, out var v) ? v : null;
        }

        private static Dictionary<string, object> BuildActionParams(string actionCode, ILookup<string, Wip.Finding> findings)
        {
            switch (actionCode)
            {
                case "ACT_PRICE_NO_COST":
                    return new Dictionary<string, object>
                    {
                        ["noCostCount"] = Param(findings, "STK_NO_COST_HIGH", "noCostCount") ?? 0,
                        ["percent"] = Param(findings, "STK_NO_COST_HIGH", "percent") ?? 0m
                    };
                case "ACT_CLEAR_AGED":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "STK_AGED_OVER_5Y", "count") ?? 0,
                        ["percent"] = Param(findings, "STK_AGED_OVER_5Y", "percent") ?? 0m,
                        ["projectedPercent"] = Param(findings, "FC_STK_AGING_PROJECTED", "toPercent") ?? 0m
                    };
                case "ACT_REPLENISH":
                case "ACT_SLOW_INTAKE":
                    return new Dictionary<string, object>
                    {
                        ["coverMonths"] = Param(findings, "FC_STK_COVER_MONTHS", "coverMonths")!,
                        ["avgMonthlyOutflow"] = Param(findings, "FC_STK_COVER_MONTHS", "avgMonthlyOutflow") ?? 0m
                    };
                case "ACT_FIX_PRODUCTION_DATE":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "STK_NO_PRODUCTION_DATE", "count") ?? 0
                    };
                default:
                    return new Dictionary<string, object>();
            }
        }
    }
}
