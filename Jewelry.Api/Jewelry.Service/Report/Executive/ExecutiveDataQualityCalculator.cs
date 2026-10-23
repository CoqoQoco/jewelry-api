using jewelry.Model.Report.Executive.DataQuality;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Report.Executive
{
    public class DataQualityInput
    {
        public string Code { get; set; } = null!;
        public int Count { get; set; }
        public int Total { get; set; }
        public decimal? AmountThb { get; set; }
        public bool Available { get; set; } = true;
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    }

    public enum DataQualitySeverityMode
    {
        // ratio > warn = warning, > critical = critical
        Ratio,

        // เหมือน Ratio แต่ไม่เกิน warning (ตัดสินใจโดย user: ราคากำลังทยอยตีเป็นรอบ)
        RatioCappedAtWarning,

        // ไม่มีตัวหารที่มีความหมาย: count > 0 = warning
        CountOnly
    }

    public class DataQualityCheckDefinition
    {
        public string Code { get; set; } = null!;
        public string FindingCode { get; set; } = null!;
        public string Tab { get; set; } = null!;
        public string ReportRef { get; set; } = null!;
        public string Impact { get; set; } = null!;
        public DataQualitySeverityMode Mode { get; set; }
    }

    // Pure — ไม่แตะ DB, รับตัวเลขที่โหลดมาแล้วเป็น argument (unit-testable)
    public static class ExecutiveDataQualityCalculator
    {
        public const string Critical = "critical";
        public const string Warning = "warning";
        public const string Ok = "ok";

        public const string NoCost = "NO_COST";
        public const string NegativeGold = "NEGATIVE_GOLD";
        public const string InvoiceNoDue = "INVOICE_NO_DUE";
        public const string SoNoDeliveryDate = "SO_NO_DELIVERY_DATE";
        public const string SoNoInvoice = "SO_NO_INVOICE";
        public const string PlaceholderWorkerWip = "PLACEHOLDER_WORKER_WIP";

        // ลำดับในรายการนี้ = ลำดับ checks ใน response
        public static readonly IReadOnlyList<DataQualityCheckDefinition> Definitions = new List<DataQualityCheckDefinition>
        {
            new() { Code = NoCost, FindingCode = "DQ_NO_COST", Tab = "stock", ReportRef = "stkCost", Impact = "stockValuation", Mode = DataQualitySeverityMode.RatioCappedAtWarning },
            new() { Code = NegativeGold, FindingCode = "DQ_NEGATIVE_GOLD", Tab = "material", ReportRef = "matItems", Impact = "goldBalance", Mode = DataQualitySeverityMode.CountOnly },
            new() { Code = InvoiceNoDue, FindingCode = "DQ_INVOICE_NO_DUE", Tab = "sales", ReportRef = "salDue", Impact = "receivableAging", Mode = DataQualitySeverityMode.Ratio },
            new() { Code = SoNoDeliveryDate, FindingCode = "DQ_SO_NO_DELIVERY", Tab = "sales", ReportRef = "salSo", Impact = "deliveryPlanning", Mode = DataQualitySeverityMode.Ratio },
            new() { Code = SoNoInvoice, FindingCode = "DQ_SO_NO_INVOICE", Tab = "sales", ReportRef = "salSo", Impact = "revenueRecognition", Mode = DataQualitySeverityMode.CountOnly },
            new() { Code = PlaceholderWorkerWip, FindingCode = "DQ_PLACEHOLDER_WIP", Tab = "production", ReportRef = "wrkTable", Impact = "workerAccountability", Mode = DataQualitySeverityMode.Ratio }
        };

        public static DataQualityCheckDefinition DefinitionOf(string code) => Definitions.First(d => d.Code == code);

        public static decimal Percent(int count, int total) => total > 0 ? (decimal)count * 100m / total : 0m;

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        public static string SeverityOf(DataQualitySeverityMode mode, int count, int total, ThresholdValues t)
        {
            if (count <= 0) return Ok;
            if (mode == DataQualitySeverityMode.CountOnly) return Warning;

            var percent = Percent(count, total);
            string severity;
            if (percent > t.CriticalPercent) severity = Critical;
            else if (percent > t.WarnPercent) severity = Warning;
            else severity = Ok;

            if (mode == DataQualitySeverityMode.RatioCappedAtWarning && severity == Critical) severity = Warning;
            return severity;
        }

        public static Check BuildCheck(DataQualityInput input, ThresholdValues t)
        {
            var def = DefinitionOf(input.Code);
            var check = new Check
            {
                Code = def.Code,
                Tab = def.Tab,
                ReportRef = def.ReportRef,
                Impact = def.Impact,
                Available = input.Available
            };

            if (!input.Available) return check;

            var percent = Percent(input.Count, input.Total);
            check.Count = input.Count;
            check.Total = input.Total;
            check.Ratio = Round2(percent);
            check.Severity = SeverityOf(def.Mode, input.Count, input.Total, t);

            foreach (var p in input.Params) check.Params[p.Key] = p.Value;
            check.Params["count"] = input.Count;
            check.Params["total"] = input.Total;
            check.Params["ratio"] = check.Ratio;
            if (input.AmountThb.HasValue) check.Params["amountThb"] = Round2(input.AmountThb.Value);
            if (def.Mode == DataQualitySeverityMode.RatioCappedAtWarning) check.Params["cappedAtWarning"] = true;
            return check;
        }

        // คืน check ครบทุกตัวตามลำดับ Definitions — code ที่ไม่มี input = available=false
        public static List<Check> BuildChecks(IEnumerable<DataQualityInput> inputs, ThresholdValues t)
        {
            var byCode = inputs.ToDictionary(i => i.Code);
            return Definitions
                .Select(d => BuildCheck(byCode.TryGetValue(d.Code, out var input) ? input : new DataQualityInput { Code = d.Code, Available = false }, t))
                .ToList();
        }

        public static Kpi BuildKpi(IReadOnlyList<Check> checks)
        {
            var available = checks.Where(c => c.Available).ToList();
            var failing = available.Where(c => c.Severity != Ok).ToList();

            return new Kpi
            {
                Total = available.Count,
                WarningCount = failing.Count(c => c.Severity == Warning),
                CriticalCount = failing.Count(c => c.Severity == Critical),
                PassedCount = available.Count - failing.Count,
                AffectedTabs = failing.Select(c => c.Tab).Distinct().ToList()
            };
        }

        public static string WorstSeverity(IEnumerable<Check> checks)
        {
            var list = checks.Where(c => c.Available).ToList();
            if (list.Any(c => c.Severity == Critical)) return Critical;
            if (list.Any(c => c.Severity == Warning)) return Warning;
            return Ok;
        }
    }
}
