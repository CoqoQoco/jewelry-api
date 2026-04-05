using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossMonthlyReport
{
    public class SearchResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Status { get; set; }
        public bool HasSavedData { get; set; }
        public decimal TotalMoneyDiff { get; set; }
        public List<GoldLossMonthlyRow> Rows { get; set; } = new List<GoldLossMonthlyRow>();
    }

    public class GoldLossMonthlyRow
    {
        public string GoldType { get; set; } = string.Empty;
        public string GoldTypeName { get; set; } = string.Empty;
        public decimal SumGoldWeightSend { get; set; }
        public decimal SumGoldWeightCheck { get; set; }
        public decimal RawLoss { get; set; }
        public decimal LossPercent { get; set; }
        public decimal GoldLossPrice { get; set; }
        public decimal WeightLossAllowed { get; set; }
        public decimal WeightLossActual { get; set; }
        public decimal MoneyDiff { get; set; }
        public string? LossRemark { get; set; }
    }
}
