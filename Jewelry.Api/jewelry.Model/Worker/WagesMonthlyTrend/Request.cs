using System;

namespace jewelry.Model.Worker.WagesMonthlyTrend
{
    public class SearchRequest
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}
