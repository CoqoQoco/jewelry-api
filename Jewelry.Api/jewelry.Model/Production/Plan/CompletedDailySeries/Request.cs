using System;

namespace jewelry.Model.Production.Plan.CompletedDailySeries
{
    public class Request
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}
