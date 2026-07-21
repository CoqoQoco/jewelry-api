namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class ReportGoldLossTangByWorkerResponse
    {
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public int SlipCount { get; set; }
        public decimal? TotalIssued { get; set; }
        public decimal? TotalReturned { get; set; }
        public decimal? TotalRawLoss { get; set; }
        public decimal? TotalAllowedLoss { get; set; }
        public decimal? TotalDiffLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
    }
}
