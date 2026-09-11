namespace jewelry.Model.Worker.GoldLossSlip
{
    public class ReportGoldLossSlipByWorkerResponse
    {
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int SlipCount { get; set; }
        public decimal TotalWeightSend { get; set; }
        public decimal TotalWeightCheck { get; set; }
        public decimal TotalWeightLossAllowed { get; set; }
        public decimal TotalWeightLossActual { get; set; }
        public decimal TotalMoneyDiff { get; set; }
        public decimal TotalGoldReturnAmount { get; set; }
    }
}
