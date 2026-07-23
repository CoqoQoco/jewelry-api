using System.Collections.Generic;

namespace jewelry.Model.Worker.WagesMonthlyTrend
{
    public class SearchResponse
    {
        public List<WagesMonthlyTrendRow> Rows { get; set; } = new List<WagesMonthlyTrendRow>();
        public WagesMonthlyTrendTotal Total { get; set; } = new WagesMonthlyTrendTotal();
    }

    public class WagesMonthlyTrendRow
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Ym { get; set; } = string.Empty;
        public int JobCount { get; set; }
        public decimal TotalWages { get; set; }
        public decimal AvgWagesPerJob { get; set; }
    }

    public class WagesMonthlyTrendTotal
    {
        public int JobCount { get; set; }
        public decimal TotalWages { get; set; }
        public decimal AvgWagesPerJob { get; set; }
    }
}
