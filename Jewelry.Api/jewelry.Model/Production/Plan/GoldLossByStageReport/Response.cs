using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossByStageReport
{
    public class SearchResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<GoldLossByStageRow> Rows { get; set; } = new List<GoldLossByStageRow>();
        public TotalRow Total { get; set; } = new TotalRow();
    }

    public class GoldLossByStageRow
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal SumGoldWeightSend { get; set; }
        public decimal SumGoldWeightCheck { get; set; }
        public decimal RawLoss { get; set; }
        public decimal RawLossPercent { get; set; }
        public int JobCount { get; set; }
    }

    public class TotalRow
    {
        public decimal SumGoldWeightSend { get; set; }
        public decimal SumGoldWeightCheck { get; set; }
        public decimal RawLoss { get; set; }
        public decimal RawLossPercent { get; set; }
        public int JobCount { get; set; }
    }
}
