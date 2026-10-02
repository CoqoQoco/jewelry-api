using System;

namespace jewelry.Model.Production.Insight.WorkerMonthly
{
    public class Request
    {
        public string Code { get; set; } = null!;
        public string DeptKey { get; set; } = null!;
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
