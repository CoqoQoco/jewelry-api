using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanGet
{
    public class PlanGetResponse
    {
        public int Id { get; set; }
        public string MoldCode { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime UpdateDate { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }
        public string? Remark { get; set; }

        public PlanGetGesign? Design { get; set; }
    }

    public class PlanGetGesign
    {
        public string MoldCode { get; set; }
        public string? Image { get; set; }

        public string? Remark { get; set; }

        public string? SizeGem { get; set; }
        public string? SizeDiamond { get; set; }
        public decimal? QtyGem { get; set; }
        public decimal? QtyDiamond { get; set; }
        public decimal? QtyBeforeSend { get; set; }
        public decimal? QtyBeforeCasting { get; set; }

    }
}
