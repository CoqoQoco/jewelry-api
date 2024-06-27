using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanDesign
{
    public class PlanDesignRequest
    {
        public string MoldCode { get; set; }
        public List<IFormFile> Images { get; set; }

        public string? Description { get; set; }

        public int? SizeGem { get; set; }
        public int? SizeDiamond { get; set; }
        public int? QtyGem { get; set; }
        public int? QtyDiamond { get; set; }
        public int? QtyBeforeSend { get; set; }
        public int? QtyBeforeCasting { get; set; }
    }
}
