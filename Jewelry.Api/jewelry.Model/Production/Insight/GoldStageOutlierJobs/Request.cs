using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.GoldStageOutlierJobs
{
    public class Request : DataSourceRequest
    {
        public string Metal { get; set; } = "GOLD";
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string[]? DepartmentKeys { get; set; }
    }
}
