using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Material.Insight;
using jewelry.Model.Material.Insight.Overview;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Material.Insight
{
    // Pure — ไม่แตะ DB, รับ item ที่คำนวณแล้วเป็น argument (unit-testable); ใช้ Finding/ActionItem shape เดียวกับ ProductionInsight
    public static class MaterialInsightRuleEngine
    {
        private const string Critical = "critical";
        private const string Warning = "warning";
        private const int TopItemCount = 3;

        private static List<string> Top(IEnumerable<Item> ordered)
            => ordered.Take(TopItemCount).Select(i => i.Label).ToList();

        private static decimal Round1(decimal v) => Math.Round(v, 1, MidpointRounding.AwayFromZero);

        // ---- problems ----

        // ยอดติดลบ (warning เสมอ) — ledger ไม่เคยมียอดตั้งต้น ไม่ใช่ของหมดจริง; เรียงตาม |ยอด| มากสุดก่อน
        public static Wip.Finding? EvaluateNegativeBalance(IReadOnlyList<Item> items, bool inboundRecorded)
        {
            var list = items.Where(i => i.Status == MaterialInsightCalculator.StatusNegative).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_NEGATIVE_BALANCE",
                Severity = Warning,
                ReportRef = "matItems",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["totalWeight"] = list.Sum(i => Math.Abs(i.OnHand)),
                    ["topItems"] = Top(list.OrderByDescending(i => Math.Abs(i.OnHand)).ThenBy(i => i.Label, StringComparer.Ordinal)),
                    ["ledgerHasInbound"] = inboundRecorded
                }
            };
        }

        // หมดสต็อก (critical) — เรียงตามอัตราใช้สูงสุดก่อน
        public static Wip.Finding? EvaluateZeroStock(IReadOnlyList<Item> items)
        {
            var list = items.Where(i => i.Status == MaterialInsightCalculator.StatusZero).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_ZERO_STOCK",
                Severity = Critical,
                ReportRef = "matItems",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["topItems"] = Top(list.OrderByDescending(i => i.AvgDailyUsage).ThenBy(i => i.Label, StringComparer.Ordinal))
                }
            };
        }

        // ต่ำกว่าขั้นต่ำแต่ยังไม่หมด (warning) — เรียงตามสัดส่วนคงเหลือ/ขั้นต่ำน้อยสุดก่อน
        public static Wip.Finding? EvaluateBelowMin(IReadOnlyList<Item> items)
        {
            var list = items.Where(i => i.Status == MaterialInsightCalculator.StatusBelow).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_BELOW_MIN",
                Severity = Warning,
                ReportRef = "matItems",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["topItems"] = Top(list.OrderBy(i => i.Available / i.EffectiveMin).ThenBy(i => i.Label, StringComparer.Ordinal))
                }
            };
        }

        public static Wip.Finding? EvaluateNearMin(IReadOnlyList<Item> items)
        {
            var list = items.Where(i => i.Status == MaterialInsightCalculator.StatusNear).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_NEAR_MIN",
                Severity = Warning,
                ReportRef = "matItems",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["topItems"] = Top(list.OrderBy(i => i.Available / i.EffectiveMin).ThenBy(i => i.Label, StringComparer.Ordinal))
                }
            };
        }

        public static Wip.Finding? EvaluateDeadStock(IReadOnlyList<Item> items, PolicyValues t)
        {
            var list = items.Where(i => i.IsDead).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_DEAD_STOCK",
                Severity = Warning,
                ReportRef = "matItems",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["totalOnHand"] = list.Sum(i => i.OnHand),
                    ["days"] = t.DeadStockDays
                }
            };
        }

        // ประเมินเฉพาะเมื่อ provider มีอายุของที่ค้างในกระบวนการ (onProcessAgeSupported) — ทองยังไม่มี => ข้าม
        public static Wip.Finding? EvaluateOnProcessStale(IReadOnlyList<Item> items, bool onProcessAgeSupported, PolicyValues t)
        {
            if (!onProcessAgeSupported) return null;

            var list = items.Where(i => i.OnProcess > 0 && i.OnProcessAgeDays.HasValue && i.OnProcessAgeDays.Value > t.OnProcessStaleDays).ToList();
            if (list.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_ON_PROCESS_STALE",
                Severity = Warning,
                ReportRef = "matOnProcess",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["totalOnProcess"] = list.Sum(i => i.OnProcess),
                    ["days"] = t.OnProcessStaleDays
                }
            };
        }

        // ---- forecasts ----

        // FC_MAT_STOCKOUT: item ที่ยังมีของ (available > 0) และมีอัตราใช้ แต่ coverDays < lead time ของ item นั้น
        // = ของจะหมดก่อนที่ของที่สั่งวันนี้จะมาถึง; warning, critical เมื่อมี item ที่ cover < ครึ่งหนึ่งของ lead time
        // gated ด้วย minHistoryMonths ของประวัติการเบิกจริง (อัตราใช้จากประวัติสั้นเกินไปไม่น่าเชื่อถือ)
        public static Wip.Finding? EvaluateStockout(IReadOnlyList<Item> items, int historyMonths, PolicyValues t)
        {
            if (historyMonths < t.MinHistoryMonths) return null;

            var list = items.Where(i => i.StockoutRisk && i.Available > 0 && i.CoverDays.HasValue).ToList();
            if (list.Count == 0) return null;

            var critical = list.Any(i => i.CoverDays!.Value < i.LeadTimeDays / 2m);
            var worst = list.OrderBy(i => i.CoverDays!.Value).ThenBy(i => i.Label, StringComparer.Ordinal).ToList();

            return new Wip.Finding
            {
                Code = "FC_MAT_STOCKOUT",
                Severity = critical ? Critical : Warning,
                ReportRef = "matTrend",
                Params = new Dictionary<string, object>
                {
                    ["count"] = list.Count,
                    ["topItems"] = Top(worst),
                    ["leadDays"] = t.LeadTimeDays,
                    ["minCoverDays"] = Round1(worst[0].CoverDays!.Value),
                    ["historyMonths"] = historyMonths,
                    ["windowMonths"] = t.WindowMonths
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
            ("ACT_RECORD_OPENING_BALANCE", "deptHead", new[] { "MAT_NEGATIVE_BALANCE" }),
            ("ACT_REORDER_MATERIAL", "planner", new[] { "MAT_ZERO_STOCK", "MAT_BELOW_MIN", "FC_MAT_STOCKOUT" }),
            ("ACT_CLEAR_DEAD_STOCK", "productionManager", new[] { "MAT_DEAD_STOCK" }),
            ("ACT_RETURN_ON_PROCESS", "deptHead", new[] { "MAT_ON_PROCESS_STALE" }),
            ("ACT_REVIEW_MIN", "planner", new[] { "MAT_NEAR_MIN" })
        };

        // priority: critical ก่อน warning แล้วตามลำดับที่ประกาศ
        public static List<Wip.ActionItem> BuildActions(IReadOnlyList<Wip.Finding> problems, IReadOnlyList<Wip.Finding> forecasts)
        {
            var byCode = problems.Concat(forecasts).ToLookup(f => f.Code);
            var built = new List<(Wip.ActionItem Action, int Rank, int Index)>();

            for (var i = 0; i < ActionDefinitions.Length; i++)
            {
                var def = ActionDefinitions[i];
                var related = def.RelatedCodes.SelectMany(c => byCode[c]).ToList();
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

        private static object? Param(ILookup<string, Wip.Finding> findings, string code, string key)
        {
            var f = findings[code].FirstOrDefault();
            return f != null && f.Params.TryGetValue(key, out var v) ? v : null;
        }

        private static Dictionary<string, object> BuildActionParams(string actionCode, ILookup<string, Wip.Finding> findings)
        {
            switch (actionCode)
            {
                case "ACT_RECORD_OPENING_BALANCE":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "MAT_NEGATIVE_BALANCE", "count") ?? 0,
                        ["totalWeight"] = Param(findings, "MAT_NEGATIVE_BALANCE", "totalWeight") ?? 0m
                    };
                case "ACT_REORDER_MATERIAL":
                    return new Dictionary<string, object>
                    {
                        ["zeroCount"] = Param(findings, "MAT_ZERO_STOCK", "count") ?? 0,
                        ["belowCount"] = Param(findings, "MAT_BELOW_MIN", "count") ?? 0,
                        ["stockoutCount"] = Param(findings, "FC_MAT_STOCKOUT", "count") ?? 0
                    };
                case "ACT_CLEAR_DEAD_STOCK":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "MAT_DEAD_STOCK", "count") ?? 0,
                        ["totalOnHand"] = Param(findings, "MAT_DEAD_STOCK", "totalOnHand") ?? 0m
                    };
                case "ACT_RETURN_ON_PROCESS":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "MAT_ON_PROCESS_STALE", "count") ?? 0,
                        ["totalOnProcess"] = Param(findings, "MAT_ON_PROCESS_STALE", "totalOnProcess") ?? 0m
                    };
                case "ACT_REVIEW_MIN":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "MAT_NEAR_MIN", "count") ?? 0
                    };
                default:
                    return new Dictionary<string, object>();
            }
        }
    }
}
