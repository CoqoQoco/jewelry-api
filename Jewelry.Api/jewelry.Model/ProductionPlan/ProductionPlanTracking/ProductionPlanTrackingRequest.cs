using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanTracking
{
    public class ProductionPlanTrackingRequest : DataSourceRequest
    {
        public ProductionPlanTracking? Search { get; set; }
    }
    public class ProductionPlanTracking
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public string? Text { get; set; }
    }
       

}
