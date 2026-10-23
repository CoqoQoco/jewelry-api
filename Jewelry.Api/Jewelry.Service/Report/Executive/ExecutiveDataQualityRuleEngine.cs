using jewelry.Model.Report.Executive.DataQuality;
using System.Collections.Generic;
using System.Linq;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Report.Executive
{
    // Pure — ไม่แตะ DB, รับ checks ที่คำนวณแล้วเป็น argument (unit-testable); ใช้ Finding/ActionItem shape เดียวกับ ProductionInsight
    public static class ExecutiveDataQualityRuleEngine
    {
        // check ที่ available และไม่ผ่าน -> 1 finding (severity เดียวกับ check) — ตามลำดับ Definitions
        public static List<Wip.Finding> BuildProblems(IReadOnlyList<Check> checks)
        {
            var problems = new List<Wip.Finding>();
            foreach (var check in checks)
            {
                if (!check.Available || check.Severity == ExecutiveDataQualityCalculator.Ok) continue;

                var def = ExecutiveDataQualityCalculator.DefinitionOf(check.Code);
                problems.Add(new Wip.Finding
                {
                    Code = def.FindingCode,
                    Severity = check.Severity,
                    ReportRef = check.ReportRef,
                    Params = new Dictionary<string, object>(check.Params)
                });
            }
            return problems;
        }

        private static readonly (string Code, string OwnerRole, string FindingCode)[] ActionDefinitions =
        {
            ("ACT_DQ_PRICE_NO_COST", "productionManager", "DQ_NO_COST"),
            ("ACT_DQ_RECORD_OPENING_BALANCE", "deptHead", "DQ_NEGATIVE_GOLD"),
            ("ACT_DQ_SET_INVOICE_DUE", "saleManager", "DQ_INVOICE_NO_DUE"),
            ("ACT_DQ_SET_SO_DELIVERY_DATE", "saleManager", "DQ_SO_NO_DELIVERY"),
            ("ACT_DQ_INVOICE_FROM_SO", "saleManager", "DQ_SO_NO_INVOICE"),
            ("ACT_DQ_ASSIGN_REAL_WORKER", "productionManager", "DQ_PLACEHOLDER_WIP")
        };

        private static int Rank(string severity) => severity == ExecutiveDataQualityCalculator.Critical ? 0 : severity == ExecutiveDataQualityCalculator.Warning ? 1 : 2;

        // priority: critical ก่อน warning แล้วตามลำดับที่ประกาศ
        public static List<Wip.ActionItem> BuildActions(IReadOnlyList<Wip.Finding> problems)
        {
            var built = new List<(Wip.ActionItem Action, int Rank, int Index)>();

            for (var i = 0; i < ActionDefinitions.Length; i++)
            {
                var def = ActionDefinitions[i];
                var related = problems.Where(f => f.Code == def.FindingCode).ToList();
                if (related.Count == 0) continue;

                var source = related[0];
                var actionParams = new Dictionary<string, object>();
                foreach (var key in new[] { "count", "total", "ratio", "amountThb" })
                {
                    if (source.Params.TryGetValue(key, out var v)) actionParams[key] = v;
                }

                built.Add((new Wip.ActionItem
                {
                    Code = def.Code,
                    OwnerRole = def.OwnerRole,
                    RelatedCodes = new List<string> { def.FindingCode },
                    Params = actionParams
                }, related.Min(f => Rank(f.Severity)), i));
            }

            var ordered = built.OrderBy(x => x.Rank).ThenBy(x => x.Index).ToList();
            for (var i = 0; i < ordered.Count; i++) ordered[i].Action.Priority = i + 1;
            return ordered.Select(x => x.Action).ToList();
        }
    }
}
