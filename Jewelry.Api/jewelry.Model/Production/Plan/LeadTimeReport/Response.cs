using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.LeadTimeReport
{
    public class SearchResponse
    {
        public string GroupBy { get; set; } = string.Empty;
        public List<LeadTimeRow> Rows { get; set; } = new List<LeadTimeRow>();
        public LeadTimeSummary Summary { get; set; } = new LeadTimeSummary();
    }

    public class LeadTimeRow
    {
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal AvgDays { get; set; }
        public decimal MedianDays { get; set; }
        public int B0_30 { get; set; }
        public int B31_90 { get; set; }
        public int B91_180 { get; set; }
        public int B181_365 { get; set; }
        public int BGt365 { get; set; }
    }

    public class LeadTimeSummary
    {
        public int TotalCount { get; set; }
        public decimal AvgDays { get; set; }
        public decimal MedianDays { get; set; }
        public int InvalidCount { get; set; }
    }
}
