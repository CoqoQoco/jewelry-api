using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.GoldLossTangSlip
{
    public class GoldLossTangSlipResponse
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
        public decimal? RawLoss { get; set; }
        public decimal? AllowedLoss { get; set; }
        public decimal? DiffLoss { get; set; }
        public decimal? TotalMoneyDiff { get; set; }
        public string? Remark { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public List<GoldLossTangSlipItemResponse> Items { get; set; } = new List<GoldLossTangSlipItemResponse>();
        public List<GoldLossTangExtraLineResponse> IssuedLines { get; set; } = new List<GoldLossTangExtraLineResponse>();
        public List<GoldLossTangExtraLineResponse> ReturnedLines { get; set; } = new List<GoldLossTangExtraLineResponse>();
        public List<GoldLossTangTypeSummaryResponse> TypeSummaries { get; set; } = new List<GoldLossTangTypeSummaryResponse>();
    }

    public class GoldLossTangSlipItemResponse
    {
        public long Id { get; set; }
        public long SlipId { get; set; }
        public int? ProductionPlanId { get; set; }
        public string? ItemNo { get; set; }
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
    }

    public class GoldLossTangExtraLineResponse
    {
        public long Id { get; set; }
        public int Kind { get; set; }
        public string? Name { get; set; }
        public decimal? Weight { get; set; }
    }

    public class GoldLossTangTypeSummaryResponse
    {
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }
        public decimal IssuedWeight { get; set; }
        public decimal ReturnedWeight { get; set; }
    }
}
