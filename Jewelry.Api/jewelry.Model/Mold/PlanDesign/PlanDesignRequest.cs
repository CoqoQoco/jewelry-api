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

        public string Catagory { get; set; }
        public string DesignBy { get; set; }

        public string? Remark { get; set; }

        //public string? SizeGem { get; set; }
        //public string? SizeDiamond { get; set; }
        //public decimal? QtyGem { get; set; }
        //public decimal? QtyDiamond { get; set; }
        public decimal QtyReceive { get; set; }
        public decimal QtySend { get; set; }

        public List<ModelGem> Gems { get; set; }

        public PlanDesignRequest()
        {
            Gems = new List<ModelGem>();
        }
    }
}
