using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.CustomerStatus
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public bool Truncated { get; set; }
        public List<CustomerItem> Customers { get; set; } = new List<CustomerItem>();
    }

    public class CustomerItem
    {
        // null = ค่า customer_number ในใบงานไม่อยู่ใน tbm_customer (free text) — ค่านั้นอยู่ใน CustomerName
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public int OpenPlanCount { get; set; }
        public int MeltedCount { get; set; }
        public List<StageCount> StageCounts { get; set; } = new List<StageCount>();
        public bool Truncated { get; set; }
        public List<PlanItem> Plans { get; set; } = new List<PlanItem>();
    }

    public class StageCount
    {
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public int Count { get; set; }
    }

    public class PlanItem
    {
        public string Wo { get; set; } = "";
        public int WoNumber { get; set; }
        public string ProductNumber { get; set; } = "";
        public string Mold { get; set; } = "";
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime LastMoveDate { get; set; }
        public int DaysSinceMove { get; set; }
    }
}
