using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatusList
{
    public class ProductionPlanStatusListRequest : DataSourceRequest
    {
      public ProductionPlanStatusList Search { get; set; }
    }

    public class ProductionPlanStatusList
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        public DateTimeOffset UpdateStart { get; set; }
        public DateTimeOffset UpdateEnd { get; set; }

        public int[]? Status { get; set; }
        public string[]? Gold { get; set; }
        
        public string? WoText { get; set; }
        public string? ProductNumber { get; set; }
    }
}
