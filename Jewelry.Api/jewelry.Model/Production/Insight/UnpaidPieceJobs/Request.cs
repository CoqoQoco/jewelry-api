using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.UnpaidPieceJobs
{
    public class Request : DataSourceRequest
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string[]? DepartmentKeys { get; set; }
    }
}
