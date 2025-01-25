using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanPrice.Transection
{
    public class TransectionResponse
    {
        public string WO { get; set; }
        public string WONumber { get; set; }

        public List<TransectionItem> Items { get; set; } = new List<TransectionItem>();
    }

    public class TransectionItem
    {
        public string Name { get; set; }
        public string NameDescription { get; set; }
        public string NameGroup { get; set; }

        public string Reference1 { get; set; }
        public string Reference2 { get; set; }
        public string Reference3 { get; set; }


        public int? Status  { get; set; }

        public DateTime? Date { get; set; }

        public decimal? Qty { get; set; }
        public decimal? QtyPrice { get; set; }

        public decimal? QtyWeight { get; set; }
        public decimal? QtyWeightPrice { get; set; }

        public decimal? PriceReference { get; set; }

        public bool IsAdd { get; set; } = false;
    }
}
