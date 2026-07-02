using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanDesign
{
    public class UpdatePlanDesignRequest
    {
        public int PlanId { get; set; }
        public string? Catagory { get; set; }
        public string? DesignBy { get; set; }
        public string? ResinBy { get; set; }
        public decimal? QtyReceive { get; set; }
        public decimal? QtySend { get; set; }
        public string? Remark { get; set; }
    }
}
