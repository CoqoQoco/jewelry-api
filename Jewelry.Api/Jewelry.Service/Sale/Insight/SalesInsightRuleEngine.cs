using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Sale.Insight.Overview;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Sale.Insight
{
    // Pure — ไม่แตะ DB, รับตัวเลขที่คำนวณแล้วเป็น argument (unit-testable); ใช้ Finding/ActionItem shape เดียวกับ ProductionInsight
    public static class SalesInsightRuleEngine
    {
        private const string Critical = "critical";
        private const string Warning = "warning";
        private const string Info = "info";

        private static string? SeverityByPercent(decimal percent, decimal warn, decimal critical)
        {
            if (percent >= critical) return Critical;
            if (percent >= warn) return Warning;
            return null;
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        // ---- problems ----

        public static Wip.Finding? EvaluateOverdueReceivable(decimal overdueThb, int overdueCount, decimal outstandingThb, ThresholdValues t)
        {
            if (outstandingThb <= 0 || overdueCount <= 0) return null;

            var percent = overdueThb * 100m / outstandingThb;
            var severity = SeverityByPercent(percent, t.OverdueWarnPercent, t.OverdueCriticalPercent);
            if (severity == null) return null;

            return new Wip.Finding
            {
                Code = "SAL_OVERDUE_RECEIVABLE",
                Severity = severity,
                ReportRef = "salAging",
                Params = new Dictionary<string, object>
                {
                    ["overdueThb"] = Round2(overdueThb),
                    ["overdueCount"] = overdueCount,
                    ["outstandingThb"] = Round2(outstandingThb),
                    ["percent"] = Round2(percent)
                }
            };
        }

        public static Wip.Finding? EvaluateOldestOverdue(int? days, string? customerName, ThresholdValues t)
        {
            if (!days.HasValue || days.Value <= t.OldestOverdueDays) return null;

            return new Wip.Finding
            {
                Code = "SAL_OLDEST_OVERDUE",
                Severity = Warning,
                ReportRef = "salDue",
                Params = new Dictionary<string, object>
                {
                    ["days"] = days.Value,
                    ["customerName"] = customerName!
                }
            };
        }

        public static Wip.Finding? EvaluateNoDueDate(int count, int unpaidCount, ThresholdValues t)
        {
            if (unpaidCount <= 0 || count <= 0) return null;

            var percent = (decimal)count * 100m / unpaidCount;
            if (percent < t.NoDueDateWarnPercent) return null;

            return new Wip.Finding
            {
                Code = "SAL_NO_DUE_DATE",
                Severity = Warning,
                ReportRef = "salDue",
                Params = new Dictionary<string, object> { ["count"] = count, ["percent"] = Round2(percent) }
            };
        }

        public static Wip.Finding? EvaluateSoNotInvoiced(int count, decimal amountThb, int overdueDeliveryCount, int noDeliveryDateCount)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "SAL_SO_NOT_INVOICED",
                Severity = Warning,
                ReportRef = "salSo",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["amountThb"] = Round2(amountThb),
                    ["overdueDeliveryCount"] = overdueDeliveryCount,
                    ["noDeliveryDateCount"] = noDeliveryDateCount
                }
            };
        }

        public static Wip.Finding? EvaluateConcentration(string? customerName, decimal? topPercent, decimal outstandingThb, ThresholdValues t)
        {
            if (outstandingThb <= 0 || !topPercent.HasValue || topPercent.Value < t.ConcentrationPercent) return null;

            return new Wip.Finding
            {
                Code = "SAL_CUSTOMER_CONCENTRATION",
                Severity = Warning,
                ReportRef = "salAging",
                Params = new Dictionary<string, object>
                {
                    ["customerName"] = customerName ?? string.Empty,
                    ["percent"] = Round2(topPercent.Value)
                }
            };
        }

        // ---- forecasts ----

        // ไม่มียอดค้าง = ไม่ emit; มียอดครบกำหนดในหน้าต่าง = info (ข้อมูลประกอบ ไม่กระทบ status);
        // มียอดค้างแต่ไม่มีบิลไหนครบกำหนดในหน้าต่าง (ไม่มี due date/เลยกำหนดหมด) = warning
        public static Wip.Finding? EvaluateCollection(decimal dueThb, int dueCount, decimal outstandingThb, int windowDays)
        {
            if (outstandingThb <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_SAL_COLLECTION",
                Severity = dueCount > 0 ? Info : Warning,
                ReportRef = "salDue",
                Params = new Dictionary<string, object>
                {
                    ["amountThb"] = Round2(dueThb),
                    ["count"] = dueCount,
                    ["windowDays"] = windowDays
                }
            };
        }

        public static Wip.Finding? EvaluateBecomingOverdue(decimal amountThb, int count, int days)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_SAL_BECOMING_OVERDUE",
                Severity = Warning,
                ReportRef = "salDue",
                Params = new Dictionary<string, object>
                {
                    ["amountThb"] = Round2(amountThb),
                    ["count"] = count,
                    ["days"] = days
                }
            };
        }

        public static Wip.Finding? EvaluateSoDelivery(int count, decimal amountThb, int days)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_SAL_SO_DELIVERY",
                Severity = Warning,
                ReportRef = "salSo",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["amountThb"] = Round2(amountThb),
                    ["days"] = days
                }
            };
        }

        // 'critical' | 'warning' | 'ok' — info ไม่ทำให้ status แย่ลง
        public static string WorstSeverity(IEnumerable<Wip.Finding> findings)
        {
            var list = findings.ToList();
            if (list.Any(f => f.Severity == Critical)) return "critical";
            if (list.Any(f => f.Severity == Warning)) return "warning";
            return "ok";
        }

        // ---- actions ----

        private static readonly (string Code, string OwnerRole, string[] RelatedCodes)[] ActionDefinitions =
        {
            ("ACT_FOLLOW_OVERDUE", "saleManager", new[] { "SAL_OVERDUE_RECEIVABLE", "SAL_OLDEST_OVERDUE", "FC_SAL_BECOMING_OVERDUE" }),
            ("ACT_SET_DUE_DATE", "saleManager", new[] { "SAL_NO_DUE_DATE" }),
            ("ACT_INVOICE_FROM_SO", "saleManager", new[] { "SAL_SO_NOT_INVOICED", "FC_SAL_SO_DELIVERY" }),
            ("ACT_SPREAD_RISK", "saleManager", new[] { "SAL_CUSTOMER_CONCENTRATION" })
        };

        // priority: critical ก่อน warning แล้วตามลำดับที่ประกาศ — topOverdueCustomerName = ลูกค้าที่มียอดเลยกำหนดสูงสุด (ใส่ใน ACT_FOLLOW_OVERDUE)
        public static List<Wip.ActionItem> BuildActions(
            IReadOnlyList<Wip.Finding> problems, IReadOnlyList<Wip.Finding> forecasts, string? topOverdueCustomerName = null)
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
                    Params = BuildActionParams(def.Code, byCode, topOverdueCustomerName)
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

        private static Dictionary<string, object> BuildActionParams(string actionCode, ILookup<string, Wip.Finding> findings, string? topOverdueCustomerName)
        {
            switch (actionCode)
            {
                case "ACT_FOLLOW_OVERDUE":
                    return new Dictionary<string, object>
                    {
                        ["overdueThb"] = Param(findings, "SAL_OVERDUE_RECEIVABLE", "overdueThb") ?? 0m,
                        ["overdueCount"] = Param(findings, "SAL_OVERDUE_RECEIVABLE", "overdueCount") ?? 0,
                        ["topCustomerName"] = topOverdueCustomerName!
                    };
                case "ACT_SET_DUE_DATE":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "SAL_NO_DUE_DATE", "count") ?? 0
                    };
                case "ACT_INVOICE_FROM_SO":
                    return new Dictionary<string, object>
                    {
                        ["count"] = Param(findings, "SAL_SO_NOT_INVOICED", "count") ?? Param(findings, "FC_SAL_SO_DELIVERY", "count") ?? 0
                    };
                case "ACT_SPREAD_RISK":
                    return new Dictionary<string, object>
                    {
                        ["customerName"] = Param(findings, "SAL_CUSTOMER_CONCENTRATION", "customerName") ?? string.Empty,
                        ["percent"] = Param(findings, "SAL_CUSTOMER_CONCENTRATION", "percent") ?? 0m
                    };
                default:
                    return new Dictionary<string, object>();
            }
        }
    }
}
