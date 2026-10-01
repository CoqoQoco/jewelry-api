using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.GoldOverSlips
{
    public class Request : DataSourceRequest
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public int[]? WorkerTypes { get; set; }
        public string[]? WorkerCodes { get; set; }

        // 'GOLD' (default) | 'SILVER'
        public string Metal { get; set; } = "GOLD";
    }
}
