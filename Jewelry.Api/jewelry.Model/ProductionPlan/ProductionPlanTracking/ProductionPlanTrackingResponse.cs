using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanTracking
{
    public class ProductionPlanTrackingResponse
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } 
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public DateTime RequestDate { get; set; }
        public string Mold { get; set; } 

        public string? ProductRunning { get; set; }
        public string ProductName { get; set; } 
        public string ProductType { get; set; } 
        public string ProductNumber { get; set; }
        public string ProductDetail { get; set; } 
        public int ProductQty { get; set; }
        public string ProductQtyUnit { get; set; } 

        public string CustomerNumber { get; set; } 
        public string? CustomerType { get; set; }

        public bool? IsActive { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? Remark { get; set; }

        public bool IsOverPlan { get; set; }
        public DateTime? LastUpdateStatus { get; set; }
    }
}
