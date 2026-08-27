using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ReportGoldLossTangMonthlyResponse
    {
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }
        public int SlipCount { get; set; }
        public List<ReportGoldLossTangMonthlySlip> Slips { get; set; } = new List<ReportGoldLossTangMonthlySlip>();
        public List<ReportGoldLossTangMonthlyGoldTypeSummary> GoldTypeSummaries { get; set; } = new List<ReportGoldLossTangMonthlyGoldTypeSummary>();
        public decimal TotalIssued { get; set; }
        public decimal TotalReturned { get; set; }
        public decimal TotalRawLoss { get; set; }
        public decimal TotalAllowedLoss { get; set; }
        public decimal TotalDiffLoss { get; set; }
        public decimal NetPayAmount { get; set; }
    }

    public class ReportGoldLossTangMonthlySlip
    {
        public long Id { get; set; }
        public string DocumentNo { get; set; }
        public DateTime? RequestDateStart { get; set; }
        public DateTime? RequestDateEnd { get; set; }
        public string? GoldSize { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? PricePerGram { get; set; }
        public decimal? IssuedTotal { get; set; }
        public decimal? ReturnedTotal { get; set; }
        public decimal? RawLoss { get; set; }
        public decimal? AllowedLoss { get; set; }
        public decimal? DiffLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
        public List<ReportGoldLossTangMonthlyItem> Items { get; set; } = new List<ReportGoldLossTangMonthlyItem>();
        public List<ReportGoldLossTangMonthlyExtra> Extras { get; set; } = new List<ReportGoldLossTangMonthlyExtra>();
    }

    public class ReportGoldLossTangMonthlyItem
    {
        public DateTime? JobDate { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductName { get; set; }
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public decimal? WeightLossAllowed { get; set; }
    }

    public class ReportGoldLossTangMonthlyExtra
    {
        public int Kind { get; set; }
        public string? Name { get; set; }
        public decimal? Weight { get; set; }
        public bool CountInCalc { get; set; }
    }

    public class ReportGoldLossTangMonthlyGoldTypeSummary
    {
        public string? GoldSize { get; set; }
        public decimal? PricePerGram { get; set; }
        public decimal IssuedTotal { get; set; }
        public decimal ReturnedTotal { get; set; }
        public decimal RawLoss { get; set; }
        public decimal AllowedLoss { get; set; }
        public decimal DiffLoss { get; set; }
        public decimal TotalMoneyDiff { get; set; }
    }
}
