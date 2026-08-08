using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Stock.Gem.Report
{
    public class MovementReportWrapperRequest : DataSourceRequest
    {
        public MovementReportRequest Search { get; set; } = new MovementReportRequest();
    }

    public class MovementReportRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public string[]? GroupName { get; set; }
        public string[]? Shape { get; set; }
        public string[]? Grade { get; set; }
        public string? Code { get; set; }
        public string[]? MovementStatus { get; set; }
        public int DeadDays { get; set; } = 180;
        public int LowDaysOfSupply { get; set; } = 30;
        public int CriticalDaysOfSupply { get; set; } = 7;
        public decimal FastTxPerMonth { get; set; } = 2;
    }
}
