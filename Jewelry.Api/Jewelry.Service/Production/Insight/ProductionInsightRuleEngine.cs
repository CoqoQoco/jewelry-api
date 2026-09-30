using System;
using System.Collections.Generic;
using System.Linq;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับตัวเลขที่คำนวณมาแล้วเข้ามาเป็น argument (unit-testable)
    public static class ProductionInsightRuleEngine
    {
        public static Wip.Finding? EvaluateWipStale(int count, int openCount)
        {
            if (count <= 0) return null;

            var percent = PercentOf(count, openCount);
            var severity = percent >= ProductionInsightThresholds.WipStalePercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_STALE",
                Severity = severity,
                ReportRef = "stalePlans",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["openCount"] = openCount,
                    ["percent"] = percent
                }
            };
        }

        public static Wip.Finding? EvaluateWipOverdue(int count, int openCount)
        {
            if (count <= 0) return null;

            var percent = PercentOf(count, openCount);
            var severity = percent >= ProductionInsightThresholds.WipOverduePercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_OVERDUE",
                Severity = severity,
                ReportRef = "dueRisk",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["openCount"] = openCount,
                    ["percent"] = percent
                }
            };
        }

        public static Wip.Finding? EvaluateWipDeptStaleTop(IReadOnlyDictionary<string, int> staleByDept, int totalStaleCount)
        {
            if (staleByDept == null || staleByDept.Count == 0) return null;

            var top = staleByDept.OrderByDescending(kv => kv.Value).First();
            if (top.Value <= 0) return null;

            var share = PercentOf(top.Value, totalStaleCount);

            return new Wip.Finding
            {
                Code = "WIP_DEPT_STALE_TOP",
                Severity = "warning",
                ReportRef = "departments",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = top.Key,
                    ["count"] = top.Value,
                    ["share"] = share
                }
            };
        }

        public static Wip.Finding? EvaluateWipMeltedOpen(int count)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "WIP_MELTED_OPEN",
                Severity = "warning",
                ReportRef = "departments",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        public static Wip.Finding? EvaluateFcBecomingStale(int count, int riskWindowDays)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_BECOMING_STALE",
                Severity = "warning",
                ReportRef = "stalePlans",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = riskWindowDays }
            };
        }

        public static Wip.Finding? EvaluateFcDueSoonAtRisk(int count, int riskWindowDays)
        {
            if (count <= 0) return null;

            var severity = count >= ProductionInsightThresholds.FcDueSoonAtRiskCountCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "FC_DUE_SOON_AT_RISK",
                Severity = severity,
                ReportRef = "dueRisk",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = riskWindowDays }
            };
        }

        public static Wip.Finding? EvaluateFcBottleneck(IReadOnlyDictionary<string, (int Inflow, int Outflow)> flowByDept)
        {
            if (flowByDept == null || flowByDept.Count == 0) return null;

            var top = flowByDept
                .Select(kv => (Key: kv.Key, Inflow: kv.Value.Inflow, Outflow: kv.Value.Outflow, Net: kv.Value.Inflow - kv.Value.Outflow))
                .OrderByDescending(x => x.Net)
                .First();

            if (top.Net <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_BOTTLENECK",
                Severity = "warning",
                ReportRef = "flow",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = top.Key,
                    ["inflow"] = top.Inflow,
                    ["outflow"] = top.Outflow,
                    ["net"] = top.Net
                }
            };
        }

        // deltaPercent = null เมื่อ startWip = 0 (คำนวณ % การเติบโตไม่ได้) — ถือว่าไม่ trigger
        // เรียกได้หลายครั้งต่อแผนก (ต่างจาก rule อื่นที่ trigger ได้ครั้งเดียว) — Code ซ้ำกันได้ในรายการ problems
        public static Wip.Finding? EvaluateWipDeptGrowing(string deptKey, int startWip, int endWip, int thresholdPercent)
        {
            if (startWip <= 0) return null;

            var deltaPercent = Math.Round((decimal)(endWip - startWip) / startWip * 100, 2);
            if (deltaPercent < thresholdPercent) return null;

            var severity = deltaPercent >= thresholdPercent * 2 ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_DEPT_GROWING",
                Severity = severity,
                ReportRef = "trend",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["startWip"] = startWip,
                    ["endWip"] = endWip,
                    ["deltaPercent"] = deltaPercent,
                    ["thresholdPercent"] = thresholdPercent
                }
            };
        }

        // 'critical' | 'warning' | 'ok' (ไม่มี finding เลย = ok)
        public static string WorstSeverity(IEnumerable<Wip.Finding> findings)
        {
            var list = findings.ToList();
            if (list.Any(f => f.Severity == "critical")) return "critical";
            if (list.Any(f => f.Severity == "warning")) return "warning";
            if (list.Any()) return "info";
            return "ok";
        }

        private static readonly (string Code, string OwnerRole, string[] RelatedCodes)[] ActionDefinitions =
        {
            ("ACT_CLOSE_STALE", "deptHead", new[] { "WIP_STALE", "FC_BECOMING_STALE" }),
            ("ACT_PRIORITIZE_DUE", "planner", new[] { "WIP_OVERDUE", "FC_DUE_SOON_AT_RISK" }),
            ("ACT_STAGE_SLA", "productionManager", new[] { "WIP_DEPT_STALE_TOP", "FC_BOTTLENECK", "WIP_DEPT_GROWING" }),
            ("ACT_CLOSE_MELTED", "goldControl", new[] { "WIP_MELTED_OPEN" })
        };

        // priority: เรียงตาม severity ที่แย่ที่สุดของ finding ที่เกี่ยวข้องก่อน (critical > warning) แล้วตามลำดับ
        // ที่ประกาศไว้ใน ActionDefinitions — ข้าม action ที่ไม่มี related code ไหนถูก trigger เลย
        // ใช้ ILookup ไม่ใช่ Dictionary เพราะ WIP_DEPT_GROWING trigger ได้หลายครั้ง (Code ซ้ำกันได้ในรายการ)
        public static List<Wip.ActionItem> BuildActions(IReadOnlyList<Wip.Finding> problems, IReadOnlyList<Wip.Finding> forecasts)
        {
            var findingsByCode = problems.Concat(forecasts).ToLookup(f => f.Code);
            var built = new List<(Wip.ActionItem Action, int SeverityRank, int DeclaredIndex)>();

            for (var i = 0; i < ActionDefinitions.Length; i++)
            {
                var def = ActionDefinitions[i];
                var relatedFindings = def.RelatedCodes
                    .SelectMany(code => findingsByCode[code])
                    .ToList();

                if (relatedFindings.Count == 0) continue;

                var severityRank = relatedFindings.Min(f => SeverityRank(f.Severity));

                var action = new Wip.ActionItem
                {
                    Code = def.Code,
                    OwnerRole = def.OwnerRole,
                    RelatedCodes = def.RelatedCodes.ToList(),
                    Params = BuildActionParams(def.Code, findingsByCode)
                };

                built.Add((action, severityRank, i));
            }

            var ordered = built.OrderBy(x => x.SeverityRank).ThenBy(x => x.DeclaredIndex).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Action.Priority = i + 1;
            }

            return ordered.Select(x => x.Action).ToList();
        }

        private static int SeverityRank(string severity) => severity switch
        {
            "critical" => 0,
            "warning" => 1,
            _ => 2
        };

        private static Dictionary<string, object> BuildActionParams(string actionCode, ILookup<string, Wip.Finding> findings)
        {
            switch (actionCode)
            {
                case "ACT_CLOSE_STALE":
                {
                    var count = GetParam(findings, "WIP_STALE", "count") ?? GetParam(findings, "FC_BECOMING_STALE", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_PRIORITIZE_DUE":
                {
                    var overdue = GetParam(findings, "WIP_OVERDUE", "count") ?? 0;
                    var dueSoon = GetParam(findings, "FC_DUE_SOON_AT_RISK", "count") ?? 0;
                    return new Dictionary<string, object> { ["overdue"] = overdue, ["dueSoon"] = dueSoon };
                }
                case "ACT_STAGE_SLA":
                {
                    var deptKey = GetParam(findings, "WIP_DEPT_STALE_TOP", "deptKey")
                        ?? GetParam(findings, "FC_BOTTLENECK", "deptKey")
                        ?? GetParam(findings, "WIP_DEPT_GROWING", "deptKey");
                    return new Dictionary<string, object> { ["deptKey"] = deptKey ?? string.Empty };
                }
                case "ACT_CLOSE_MELTED":
                {
                    var count = GetParam(findings, "WIP_MELTED_OPEN", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                default:
                    return new Dictionary<string, object>();
            }
        }

        // เอา finding แรกของ code นั้น (เรียงตามลำดับแผนกที่ declare ไว้ — เพียงพอสำหรับ param เดี่ยวแบบนี้)
        private static object? GetParam(ILookup<string, Wip.Finding> findings, string code, string paramKey)
        {
            var finding = findings[code].FirstOrDefault();
            return finding != null && finding.Params.TryGetValue(paramKey, out var value) ? value : null;
        }

        private static decimal PercentOf(int part, int total)
        {
            return total > 0 ? Math.Round((decimal)part / total * 100, 1) : 0m;
        }
    }
}
