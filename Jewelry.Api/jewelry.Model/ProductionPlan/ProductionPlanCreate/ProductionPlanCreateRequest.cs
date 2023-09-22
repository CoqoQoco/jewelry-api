using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanCreate
{
    public class ProductionPlanCreateRequest
    {
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public DateTimeOffset RequestDate { get; set; }

        public string Mold { get; set; }
        public string ProductNumber { get; set; }
        public string ProductDetail { get; set; }
        public string CustomerNumber { get; set; }

        public int Qty { get; set; }
        public int? QtyFinish { get; set; }
        public int? QtySemiFinish { get; set; }
        public int? QtyCast { get; set; }
        public string QtyUnit { get; set; }

        public string? Remark { get; set; }

        public string Material { get; set; }

        //public List<ProductionPlanMaterialCreateRequest> Material { get; set; }
        public IFormFile Images { get; set; }

        public ProductionPlanCreateRequest()
        {
            //Material = new List<ProductionPlanMaterialCreateRequest> ();
            //Images = new List<IFormFile>();
        }
    }

    public class ProductionPlanMaterialCreateRequest
    {
        public string Material { get; set; }
        public string MaterialType { get; set; }
        public string MaterialShape { get; set; }
        public string MaterialSize { get; set; }
        public string MaterialQty { get; set; }
        public string? MaterialRemark { get; set; }
    }
}
