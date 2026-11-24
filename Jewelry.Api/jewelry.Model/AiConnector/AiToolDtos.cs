using System;
using System.Collections.Generic;

namespace jewelry.Model.AiConnector
{
    // AI DTO: ทุก property ต้องติด [AiSensitive] หรือ [AiNotSensitive] · ห้าม object / Dictionary / dynamic
    // (มีเทส reflection ตรวจ) — แยกจาก response ของเว็บโดยตั้งใจ

    public class AiGlossaryInput
    {
        public string? Module { get; set; }
        public string? Metric { get; set; }
    }

    public class AiGlossaryItem
    {
        [AiNotSensitive] public string MetricCode { get; set; } = "";
        [AiNotSensitive] public string Module { get; set; } = "";
        [AiNotSensitive] public string NameTh { get; set; } = "";
        [AiNotSensitive] public string DefinitionTh { get; set; } = "";
        [AiNotSensitive] public string? CaveatTh { get; set; }
    }

    public class AiGlossaryOutput
    {
        [AiNotSensitive] public List<AiGlossaryItem> Items { get; set; } = new List<AiGlossaryItem>();
    }

    public class AiMetalInput
    {
        // ALL (default) | GOLD | SILVER
        public string? Metal { get; set; }
    }

    public class AiProductionSummary
    {
        [AiNotSensitive] public int OpenCount { get; set; }
        [AiNotSensitive] public int Moved30dCount { get; set; }
        [AiNotSensitive] public int Stale180dCount { get; set; }
        [AiNotSensitive] public int MeltedOpenCount { get; set; }
    }

    public class AiReceivablesSummary
    {
        [AiNotSensitive] public int InvoiceCount { get; set; }
        [AiNotSensitive] public int UnpaidCount { get; set; }
        [AiNotSensitive] public int OverdueCount { get; set; }
        [AiNotSensitive] public int NoDueDateCount { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? InvoiceTotalThb { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? PaidOrPartialThb { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? UnpaidThb { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? OverdueThb { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? UnpaidNotOverdueThb { get; set; }
    }

    public class AiSalesOrdersSummary
    {
        [AiNotSensitive] public int OpenCount { get; set; }
        [AiNotSensitive] public int NoInvoiceCount { get; set; }
        [AiNotSensitive] public int OverdueNoInvoiceCount { get; set; }
        [AiNotSensitive] public int NoDeliveryDateCount { get; set; }
        [AiSensitive(SensitiveCategory.Receivable)] public decimal? NoInvoiceThb { get; set; }
    }

    public class AiStockSummary
    {
        [AiNotSensitive] public int InStockCount { get; set; }
        [AiNotSensitive] public int InStockLotCount { get; set; }
        [AiNotSensitive] public int NoCostLotCount { get; set; }
        [AiNotSensitive] public int ReservedCount { get; set; }
        [AiNotSensitive] public int NoCostCount { get; set; }
        [AiNotSensitive] public int AgedOver1yCount { get; set; }
        [AiSensitive(SensitiveCategory.Cost)] public decimal? CostThb { get; set; }
    }

    public class AiGoldLossMonth
    {
        [AiNotSensitive] public string Month { get; set; } = "";
        [AiNotSensitive] public int SlipCount { get; set; }
        [AiNotSensitive] public decimal IssuedGram { get; set; }
        [AiNotSensitive] public decimal RawLossGram { get; set; }
        [AiNotSensitive] public decimal AllowedGram { get; set; }
        [AiNotSensitive] public decimal OverAllowedGram { get; set; }
        [AiNotSensitive] public int OverSlipCount { get; set; }
        [AiNotSensitive] public decimal LossPercent { get; set; }
        [AiNotSensitive] public decimal AllowedPercent { get; set; }
        [AiNotSensitive] public decimal OverAllowedPercent { get; set; }
    }

    public class AiExecutiveSummaryOutput
    {
        [AiNotSensitive] public DateTime AsOf { get; set; }
        [AiNotSensitive] public string Metal { get; set; } = "ALL";
        [AiNotSensitive] public AiProductionSummary Production { get; set; } = new AiProductionSummary();
        [AiNotSensitive] public AiReceivablesSummary Receivables { get; set; } = new AiReceivablesSummary();
        [AiNotSensitive] public AiSalesOrdersSummary SalesOrders { get; set; } = new AiSalesOrdersSummary();
        [AiNotSensitive] public AiStockSummary Stock { get; set; } = new AiStockSummary();
        [AiNotSensitive] public List<AiGoldLossMonth> GoldLoss { get; set; } = new List<AiGoldLossMonth>();
        [AiNotSensitive] public List<AiGoldLossMonth> SilverLoss { get; set; } = new List<AiGoldLossMonth>();
    }

    public class AiDigestDomain
    {
        [AiNotSensitive] public string Key { get; set; } = "";
        [AiNotSensitive] public string NameTh { get; set; } = "";
        [AiNotSensitive] public string Status { get; set; } = "unknown";
        [AiNotSensitive] public int ProblemCount { get; set; }
        [AiNotSensitive] public int ForecastCount { get; set; }
        [AiNotSensitive] public bool Available { get; set; }
    }

    public class AiMetric
    {
        [AiNotSensitive] public string Name { get; set; } = "";
        [AiNotSensitive] public decimal Value { get; set; }
    }

    public class AiDigestItem
    {
        [AiNotSensitive] public string Domain { get; set; } = "";
        [AiNotSensitive] public string DomainTh { get; set; } = "";
        [AiNotSensitive] public string Severity { get; set; } = "";
        [AiNotSensitive] public string Tab { get; set; } = "";
        [AiNotSensitive] public string Code { get; set; } = "";
        [AiNotSensitive] public string Message { get; set; } = "";
        [AiNotSensitive] public List<AiMetric> Metrics { get; set; } = new List<AiMetric>();
        [AiSensitive(SensitiveCategory.Receivable)] public List<AiMetric> ReceivableMetrics { get; set; } = new List<AiMetric>();
        [AiSensitive(SensitiveCategory.Cost)] public List<AiMetric> CostMetrics { get; set; } = new List<AiMetric>();
    }

    public class AiExecutiveDigestOutput
    {
        [AiNotSensitive] public DateTime AsOf { get; set; }
        [AiNotSensitive] public string Metal { get; set; } = "ALL";
        [AiNotSensitive] public List<AiDigestDomain> Domains { get; set; } = new List<AiDigestDomain>();
        [AiNotSensitive] public List<AiDigestItem> Top { get; set; } = new List<AiDigestItem>();
    }

    // ซองที่ pipeline ห่อผลของทุก tool
    public class AiDefinition
    {
        public string Metric { get; set; } = "";
        public string Name { get; set; } = "";
        public string Definition { get; set; } = "";
    }

    public class AiDataQualityNote
    {
        public string Metric { get; set; } = "";
        public string Caveat { get; set; } = "";
    }
}
