using jewelry.Model.Master;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanUpdate
{
    public class ProductionPlanUpdateMaterialRequest
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public ProductionPlanUpdateMaterial Material { get; set; }
    }

    public class ProductionPlanUpdateMaterial
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
