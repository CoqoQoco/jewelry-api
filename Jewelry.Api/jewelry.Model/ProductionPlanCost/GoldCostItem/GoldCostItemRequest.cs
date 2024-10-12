using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlanCost.GoldCostItem
{
    public class GoldCostItemRequest : DataSourceRequest
    {
       public GoldCostItemSearch Search { get; set; }
    }

    public class GoldCostItemSearch
    {
        public string? ProductionPlanNumber { get; set; }
        public string? RunningNumber { get; set; }
        public string? BookNo { get; set; }
        public string? No { get; set; }
    }
}
