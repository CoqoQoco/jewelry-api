using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.PrePlan.FunnelReport;

public class SearchRequest
{
    public DateTimeOffset? Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public List<string>? JobType { get; set; }
    public List<string>? JobLocation { get; set; }
}

public class SearchResponse
{
    public List<FunnelStatusRow> StatusRows { get; set; } = new List<FunnelStatusRow>();
    public FunnelSummary Summary { get; set; } = new FunnelSummary();
}

public class FunnelStatusRow
{
    public string Status { get; set; } = null!;
    public int OrderIndex { get; set; }
    public int Count { get; set; }
    public decimal Percent { get; set; }
}

public class FunnelSummary
{
    public int Total { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal CancelRate { get; set; }
    public decimal AvgOrderToSubmitDays { get; set; }
    public decimal AvgSubmitToApproveDays { get; set; }
}
