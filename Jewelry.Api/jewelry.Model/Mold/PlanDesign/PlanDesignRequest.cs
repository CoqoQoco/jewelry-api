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

        public string? Remark { get; set; }

        public string? SizeGem { get; set; }
        public string? SizeDiamond { get; set; }
        public decimal? QtyGem { get; set; }
        public decimal? QtyDiamond { get; set; }
        public decimal? QtyBeforeSend { get; set; }
        public decimal? QtyBeforeCasting { get; set; }
    }
}
