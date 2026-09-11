using System;

namespace jewelry.Model.Production.Plan.GoldLossReconcileReport
{
    public class SearchRequest
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public int[]? Status { get; set; }
        public string? WorkerCode { get; set; }
    }
}
