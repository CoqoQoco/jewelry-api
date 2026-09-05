using System;

namespace jewelry.Model.Production.Plan.GoldLossByWorkerReport
{
    public class Request
    {
        public Criteria Search { get; set; } = new Criteria();
    }

    public class Criteria
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public int[]? Status { get; set; }
        public string? WorkerCode { get; set; }
        public string[]? Gold { get; set; }
        public int? MinJobCount { get; set; }
    }
}
