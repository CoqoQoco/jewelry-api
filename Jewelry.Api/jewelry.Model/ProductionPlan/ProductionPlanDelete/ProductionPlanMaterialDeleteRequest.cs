using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanDelete
{
    public class ProductionPlanMaterialDeleteRequest
    {
        public int PlanId { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public int MaterialId { get; set; }
    }
}
