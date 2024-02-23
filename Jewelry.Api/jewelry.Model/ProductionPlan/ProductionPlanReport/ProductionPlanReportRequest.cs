using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanReport
{
    public class ProductionPlanReportRequest : DataSourceRequest
    {
        public ProductionPlanReport Search { get; set; }
    }

    public class ProductionPlanReport
    {
        public DateTimeOffset CreateStart { get; set; }
        public DateTimeOffset CreateEnd { get; set; }
        public string? WoText { get; set; }
    }
}
