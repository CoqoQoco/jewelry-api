using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlanCost.GoldCostList
{
    public class GoldCostListRequest : DataSourceRequest
    {
        public GoldCostList Search { get; set; }
    }

    public class GoldCostList
    {
        public string? Text { get; set; }
    }
}
