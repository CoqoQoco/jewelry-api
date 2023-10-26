using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanStatus
{
    public class ProductionPlanStatusDeleteRequest
    {
        public int ProductionPlanId { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public int Id { get; set; }
    }
}
