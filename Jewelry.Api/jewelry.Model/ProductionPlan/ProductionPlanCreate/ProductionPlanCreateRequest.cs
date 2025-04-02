using jewelry.Model.Master;
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
        public string Mold { get; set; }

        public string CustomerNumber { get; set; }
        public string CustomerType { get; set; }
        public DateTimeOffset RequestDate { get; set; }

        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }

        public int ProductQty { get; set; }
        public string ProductQtyUnit { get; set; }

        public string ProductDetail { get; set; }
        public string? Remark { get; set; }

        
        public string? Gold { get; set; }
        public string? GoldSize { get; set; }

        public bool IsModifyPlan { get; set; } = false;

        public string Material { get; set; }

        //public List<ProductionPlanMaterialCreateRequest> Material { get; set; }

        public ProductionPlanCreateRequest()
        {
            //Material = new List<ProductionPlanMaterialCreateRequest> ();
            //Images = new List<IFormFile>();
        }
    }

    public class ProductionPlanMaterialCreateRequest
    {
        public MasterModel Gold { get; set; }
        public MasterModel? GoldSize { get; set; }
        public int? GoldQty { get; set; }

        public MasterModel? Gem { get; set; }
        public MasterModel? GemShape { get; set; }
        public int? GemQty { get; set; }
        public string? GemUnit { get; set; }
        public string? GemSize { get; set; }
        public string? GemWeight { get; set; }
        public string? GemWeightUnit { get; set; }

        public int? DiamondQty { get; set; }
        public string? DiamondUnit { get; set; }
        public string? DiamondQuality { get; set; }
        public string? DiamondWeight { get; set; }
        public string? DiamondWeightUnit { get; set; }
        public string? DiamondSize { get; set; }
    }
}
