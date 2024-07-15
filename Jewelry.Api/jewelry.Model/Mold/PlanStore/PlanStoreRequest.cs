using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanStore
{
    public class PlanStoreRequest
    {
        public int PlanId { get; set; }
        public string MoldCode { get; set; }
        public string Code { get; set; }

        public decimal? QtyReceive { get; set; }
        public decimal? QtySend { get; set; }

        public string WorkerBy { get; set; }
        public string? Remark { get; set; }

        public string Location { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}
