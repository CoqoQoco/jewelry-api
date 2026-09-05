using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.StageLeadTimeReport
{
    public class Response
    {
        public List<StageRow> Rows { get; set; } = new List<StageRow>();
        public List<WipRow> WipRows { get; set; } = new List<WipRow>();
        public List<StuckJob> TopStuckJobs { get; set; } = new List<StuckJob>();
        public StageLeadTimeSummary Summary { get; set; } = new StageLeadTimeSummary();
    }

    public class StageRow
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int VisitCount { get; set; }
        public decimal AvgDays { get; set; }
        public decimal MedianDays { get; set; }
        public decimal P90Days { get; set; }
        public decimal TotalDays { get; set; }
        public decimal ShareOfTotalPercent { get; set; }
        public decimal MedianWorkDays { get; set; }
        public decimal WorkDataReliabilityPercent { get; set; }
    }

    public class WipRow
    {
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int WipCount { get; set; }
        public decimal AvgAgeDays { get; set; }
        public decimal MaxAgeDays { get; set; }
    }

    public class StuckJob
    {
        public int ProductionPlanId { get; set; }
        public string WoText { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public decimal AgeDays { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime RequestDate { get; set; }
    }

    public class StageLeadTimeSummary
    {
        public int CompletedPlanCount { get; set; }
        public decimal AvgTotalLeadDays { get; set; }
        public decimal MedianTotalLeadDays { get; set; }
        public int BottleneckStatusCode { get; set; }
        public string BottleneckStatusName { get; set; } = string.Empty;
        public int PlansWithNoStageCount { get; set; }
    }
}
