using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossTangReport
{
    public class SearchRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? Wo { get; set; }
        public string? WorkerCode { get; set; }
        public string? GoldCode { get; set; }
        public int Status { get; set; } = 50;
    }

    public class SaveRequest
    {
        public List<GoldLossTangSaveItem> Items { get; set; } = new List<GoldLossTangSaveItem>();
    }

    public class GoldLossTangSaveItem
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; } = string.Empty;
        public decimal? LossPercent { get; set; }
        public decimal? GoldLossPrice { get; set; }
        public string? LossRemark { get; set; }
    }

    public class CreateJobRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? Remark { get; set; }
        public List<GoldLossTangJobItem> Items { get; set; } = new();
    }

    public class JobListRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string? DocumentNo { get; set; }
    }

    public class UpdateJobRequest
    {
        public int JobId { get; set; }
        public string? Remark { get; set; }
        public List<GoldLossTangJobItem> Items { get; set; } = new();
    }

    public class JobDetailRequest
    {
        public int JobId { get; set; }
    }

    public class GoldLossTangJobItem
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; } = string.Empty;
        public string? Wo { get; set; }
        public string? WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public string? Gold { get; set; }
        public decimal GoldQtySend { get; set; }
        public decimal GoldWeightSend { get; set; }
        public decimal GoldQtyCheck { get; set; }
        public decimal GoldWeightCheck { get; set; }
        public decimal LossPercent { get; set; }
        public decimal GoldLossPrice { get; set; }
        public decimal WeightLossAllowed { get; set; }
        public decimal WeightLossActual { get; set; }
        public decimal MoneyDiff { get; set; }
        public string? LossRemark { get; set; }
        public DateTime? RequestDate { get; set; }
        public int WoNumber { get; set; }
        public string? WoText { get; set; }
    }
}
