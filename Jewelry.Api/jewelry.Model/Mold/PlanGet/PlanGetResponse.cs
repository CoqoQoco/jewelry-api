using Microsoft.AspNetCore.Http;
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
        public int NextStatus { get; set; }
        public string NextStatusName { get; set; }

        public string? Remark { get; set; }

        public List<ModelGem> Gems { get; set; }

        public PlanGetItemStatus? Design { get; set; }
        public PlanGetItemStatus? Resin { get; set; }
        public PlanGetItemStatus? CastingSilver { get; set; }
        public PlanGetItemStatus? Casting{ get; set; }
        public PlanGetItemStatus? Cutting { get; set; }
        public PlanGetItemStatus? Store { get; set; }
    }

    public class PlanGetItemStatus
    {
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public decimal? QtyReceive { get; set; }
        public decimal? QtySend { get; set; }

        public string? WorkBy { get; set; }
        public string? Remark { get; set; }

        public string? Image { get; set; }

        public string? SizeGem { get; set; }
        public string? SizeDiamond { get; set; }
        public decimal? QtyGem { get; set; }
        public decimal? QtyDiamond { get; set; }

        public string? Catagory { get; set; }
        public string? CatagoryName
        {
            get; set;
        }

        public string? MoldCode { get; set; }
        public string? Code { get; set; }

        public string? Location { get; set; }
    }
}
