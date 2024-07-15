using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanCastingSilver
{
    public class PlanCastingSilverRequest
    {
        public int PlanId { get; set; }
        public string MoldCode { get; set; }

        public decimal QtyReceive { get; set; }
        public decimal QtySend { get; set; }

        public string CastBy { get; set; }
        public string? Remark { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}
