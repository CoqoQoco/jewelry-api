using System.Collections.Generic;

namespace jewelry.Model.Worker.WagesByProcess
{
    public class SearchResponse
    {
        public List<WagesByProcessRow> Rows { get; set; } = new List<WagesByProcessRow>();
        public WagesByProcessTotal Total { get; set; } = new WagesByProcessTotal();
    }

    public class WagesByProcessRow
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int JobCount { get; set; }
        public decimal TotalWages { get; set; }
        public decimal AvgWagesPerJob { get; set; }
    }

    public class WagesByProcessTotal
    {
        public int JobCount { get; set; }
        public decimal TotalWages { get; set; }
        public decimal AvgWagesPerJob { get; set; }
    }
}
