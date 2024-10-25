using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanPrice.CreatePrice
{
    public class CreatePriceRequest
    {
        public int ProductionPlanId { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }
        public IEnumerable<CreatePriceItem> Item { get; set; }
    }

    public class CreatePriceItem
    { 
        public int No { get; set; }

        public string Name { get; set; }
        public string NameDescription { get; set; }
        public string NameGroup { get; set; }

        public DateTimeOffset? Date { get; set; }

        public decimal Qty { get; set; }
        public decimal QtyPrice { get; set; }

        public decimal QtyWeight { get; set; }
        public decimal QtyWeightPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}
