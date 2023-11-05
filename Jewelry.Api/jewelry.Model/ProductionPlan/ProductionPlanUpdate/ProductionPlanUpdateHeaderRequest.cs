using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanUpdate
{
    public class ProductionPlanUpdateHeaderRequest
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public string Mold { get; set; }
        public DateTimeOffset? RequestDate { get; set; }

        public string CustomerNumber { get; set; }
        public string? CustomerType { get; set; }

        public int ProductQty { get; set; }
        public string ProductQtyUnit { get; set; }

        public string ProductName { get; set; }
        public string? ProductType { get; set; }
        public string ProductDetail { get; set; }
        public string? Remark { get; set; }
    }
}
