using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ReportGoldLossTangByWorkerResponse
    {
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int SlipCount { get; set; }
        public decimal? TotalIssued { get; set; }
        public decimal? TotalReturned { get; set; }
        public decimal? TotalRawLoss { get; set; }
        public decimal? TotalAllowedLoss { get; set; }
        public decimal? TotalDiffLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
        public List<GoldTypeBreakdownRow> ByGoldType { get; set; } = new List<GoldTypeBreakdownRow>();
    }

    public class GoldTypeBreakdownRow
    {
        public string? GoldSize { get; set; }
        public decimal? PricePerGram { get; set; }
        public bool HasMixedPrice { get; set; }
        public decimal IssuedTotal { get; set; }
        public decimal ReturnedTotal { get; set; }
        public decimal RawLoss { get; set; }
        public decimal AllowedLoss { get; set; }
        public decimal DiffLoss { get; set; }
        public decimal MoneyDiff { get; set; }
        public int SlipCount { get; set; }
    }
}
