using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlan.ProductionPlanUpdate
{
    public class ProductionPlanUpdateRequest
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public int Status { get; set; }
    }
}
