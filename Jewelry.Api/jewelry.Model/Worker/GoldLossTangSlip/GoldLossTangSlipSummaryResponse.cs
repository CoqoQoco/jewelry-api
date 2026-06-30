using System;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class GoldLossTangSlipSummaryResponse
    {
        public long Id { get; set; }
        public string DocumentNo { get; set; }
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public DateTime? RequestDateStart { get; set; }
        public DateTime? RequestDateEnd { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? PricePerGram { get; set; }
        public decimal? IssuedTotal { get; set; }
        public decimal? ReturnedTotal { get; set; }
        public decimal? DiffLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
        public string? Remark { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
