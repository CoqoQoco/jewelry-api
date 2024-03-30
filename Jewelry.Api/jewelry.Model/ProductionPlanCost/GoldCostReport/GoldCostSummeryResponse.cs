using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlanCost.GoldCostReport
{
    public class GoldCostSummeryResponse
    {
        public decimal? TotalMeltWeight { get; set; }
        public decimal? TotalReturnMeltWeight { get; set; }
        public decimal? TotalReturnMeltScrapWeight { get; set; }
        public decimal? TotalMeltWeightLoss { get; set; }
        public decimal? TotalMeltWeightOver { get; set; }
        public decimal? TotalCastWeight { get; set; }
        public decimal? TotalGemWeight { get; set; }
        public decimal? TotalReturnCastWeight { get; set; }
        public decimal? TotalReturnCastMoldWeight { get; set; }
        public decimal? TotalReturnCastBodyWeightTotal { get; set; }
        public decimal? TotalReturnCastBodyBrokenWeight { get; set; }
        public decimal? TotalReturnCastScrapWeight { get; set; }
        public decimal? TotalReturnCastPowderWeight { get; set; }
        public decimal? TotalCastWeightLoss { get; set; }
        public decimal? TotalCastWeightOver { get; set; }
    }
}
