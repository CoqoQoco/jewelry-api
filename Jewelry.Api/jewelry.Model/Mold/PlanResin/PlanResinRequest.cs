using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanResin
{
    public class PlanResinRequest
    {
        public int PlanId { get; set; }
        public string MoldCode { get; set; }

        public decimal QtyReceive { get; set; }
        public decimal QtySend { get; set; }

        public string ResinBy { get; set; }
        public string? Remark { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}
