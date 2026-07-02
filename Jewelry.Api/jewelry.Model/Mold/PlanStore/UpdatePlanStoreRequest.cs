using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanStore
{
    public class UpdatePlanStoreRequest
    {
        public int PlanId { get; set; }
        public string Code { get; set; }
        public string? Location { get; set; }
        public string? WorkerBy { get; set; }
        public string? PrintBy { get; set; }
        public string? CuttingBy { get; set; }
        public decimal? QtyReceive { get; set; }
        public decimal? QtySend { get; set; }
        public decimal? QtyResin { get; set; }
        public decimal? QtySilverCast { get; set; }
        public string? Remark { get; set; }
    }
}
