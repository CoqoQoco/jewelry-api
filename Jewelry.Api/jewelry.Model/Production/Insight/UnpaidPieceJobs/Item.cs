using System;

namespace jewelry.Model.Production.Insight.UnpaidPieceJobs
{
    public class Item
    {
        public int PlanId { get; set; }
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string WoText { get; set; } = null!;
        public string DeptKey { get; set; } = null!;
        public string? WorkerCode { get; set; }
        public string? WorkerName { get; set; }
        public DateTime JobDate { get; set; }
        public decimal CheckGram { get; set; }
    }
}
