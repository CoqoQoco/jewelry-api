using System.Collections.Generic;

namespace jewelry.Model.ProductionPlanCost.CastLossTrend
{
    public class SearchResponse
    {
        public List<CastLossTrendRow> Rows { get; set; } = new List<CastLossTrendRow>();
        public CastLossTrendTotal Total { get; set; } = new CastLossTrendTotal();
    }

    public class CastLossTrendRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Ym { get; set; }
        public int BookCount { get; set; }
        public decimal SumCastWeight { get; set; }
        public decimal SumCastLoss { get; set; }
        public decimal CastLossPct { get; set; }
        public decimal SumCastOver { get; set; }
        public decimal CastOverPct { get; set; }
    }

    public class CastLossTrendTotal
    {
        public int BookCount { get; set; }
        public decimal SumCastWeight { get; set; }
        public decimal SumCastLoss { get; set; }
        public decimal CastLossPct { get; set; }
        public decimal SumCastOver { get; set; }
        public decimal CastOverPct { get; set; }
    }
}
