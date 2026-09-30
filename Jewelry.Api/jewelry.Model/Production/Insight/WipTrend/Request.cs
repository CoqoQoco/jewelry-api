using System;

namespace jewelry.Model.Production.Insight.WipTrend
{
    public class Request
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";
    }
}
