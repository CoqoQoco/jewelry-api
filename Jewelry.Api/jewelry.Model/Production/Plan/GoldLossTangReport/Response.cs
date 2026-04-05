using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossTangReport
{
    public class SearchResponse
    {
        public bool HasSavedData { get; set; }
        public List<GoldLossTangDetailRow> Rows { get; set; } = new List<GoldLossTangDetailRow>();
    }

    public class JobListRow
    {
        public int Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Status { get; set; }
        public string? Remark { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalMoneyDiff { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
    }

    public class JobDetailResponse
    {
        public int Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Status { get; set; }
        public string? Remark { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public List<GoldLossTangDetailRow> Items { get; set; } = new();
    }

    public class GoldLossTangDetailRow
    {
        public int ProductionPlanId { get; set; }
        public string ItemNo { get; set; } = string.Empty;

        public string Wo { get; set; } = string.Empty;
        public int WoNumber { get; set; }
        public string WoText { get; set; }
    

        public string WorkerCode { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string GoldType { get; set; } = string.Empty;
        public string GoldTypeName { get; set; } = string.Empty;
        public DateTime? RequestDate { get; set; }
        public decimal GoldQtySend { get; set; }
        public decimal GoldWeightSend { get; set; }
        public decimal GoldQtyCheck { get; set; }
        public decimal GoldWeightCheck { get; set; }
        public decimal LossPercent { get; set; }
        public decimal GoldLossPrice { get; set; }
        public string? LossRemark { get; set; }
    }
}
