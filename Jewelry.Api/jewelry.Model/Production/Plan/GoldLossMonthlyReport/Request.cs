using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.GoldLossMonthlyReport
{
    public class SearchRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Status { get; set; } = 50;
    }

    public class SaveRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Status { get; set; } = 50;
        public List<GoldLossMonthlyItem> Items { get; set; } = new List<GoldLossMonthlyItem>();
    }

    public class GoldLossMonthlyItem
    {
        public string GoldType { get; set; } = string.Empty;
        public decimal? LossPercent { get; set; }
        public decimal? GoldLossPrice { get; set; }
        public string? LossRemark { get; set; }
    }
}
