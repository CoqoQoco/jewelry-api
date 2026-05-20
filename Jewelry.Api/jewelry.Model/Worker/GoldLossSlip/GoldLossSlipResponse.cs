using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossSlip
{
    public class GoldLossSlipResponse
    {
        public long Id { get; set; }
        public string DocumentNo { get; set; }
        public string WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public DateTime RequestDateStart { get; set; }
        public DateTime RequestDateEnd { get; set; }
        public decimal? GoldReturn { get; set; }
        public decimal? TotalWeightLoss { get; set; }
        public decimal? NetWeightLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
        public string? Remark { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public List<GoldLossSlipItemResponse> Items { get; set; } = new List<GoldLossSlipItemResponse>();
    }

    public class GoldLossSlipItemResponse
    {
        public long Id { get; set; }
        public long SlipId { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductName { get; set; }
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }
        public DateTime? JobDate { get; set; }
        public decimal? GoldQtySend { get; set; }
        public decimal? GoldWeightSend { get; set; }
        public decimal? GoldQtyCheck { get; set; }
        public decimal? GoldWeightCheck { get; set; }
        public decimal? LossPercent { get; set; }
        public decimal? WeightLossAllowed { get; set; }
        public decimal? WeightLossActual { get; set; }
        public decimal? GoldLossPrice { get; set; }
        public decimal? MoneyDiff { get; set; }
    }
}
