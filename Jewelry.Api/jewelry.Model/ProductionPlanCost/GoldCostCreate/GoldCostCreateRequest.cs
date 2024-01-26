using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlanCost.GoldCostCreate
{
    public  class GoldCostCreateRequest
    {
        public string No { get; set; }
        public string BookNo { get; set; }
        public DateTimeOffset AssignDateFormat { get; set; }

        public string GoldCode { get; set; }
        public string GoldSizeCode { get; set; }
        public string GoldReceipt { get; set; }

        public DateTimeOffset? MeltDateFormat { get; set; }
        public decimal? MeltWeight { get; set; }
        public decimal? ReturnMeltWeight { get; set; }
        public decimal? ReturnMeltScrapWeight { get; set; }
        public decimal? MeltWeightLoss { get; set; }
        public decimal? MeltWeightOver { get; set; }

        public DateTimeOffset? CastDateFormat { get; set; }
        public decimal? CastWeight { get; set; }
        public decimal? GemWeight { get; set; }
        public decimal? ReturnCastWeight { get; set; }
        public decimal? ReturnCastBodyWeight { get; set; }
        public decimal? ReturnCastScrapWeight { get; set; }
        public decimal? ReturnCastPowderWeight { get; set; }
        public decimal? CastWeightLoss { get; set; }
        public decimal? CastWeightOver { get; set; }

        public string? Remark { get; set; }

        public string? AssignBy { get; set; }
        public string? ReceiveBy { get; set; }
    }
}
