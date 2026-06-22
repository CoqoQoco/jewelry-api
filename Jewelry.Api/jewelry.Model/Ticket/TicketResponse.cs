using System;
using System.Collections.Generic;

namespace jewelry.Model.Ticket;

public class TicketListResponse
{
    public long Id { get; set; }
    public string TicketNo { get; set; } = null!;
    public int Type { get; set; }
    public string TopicRoute { get; set; } = null!;
    public string TopicName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ScreenshotUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public int StatusId { get; set; }
    public string StatusNameTh { get; set; } = null!;
    public string StatusNameEn { get; set; } = null!;
    public string? DevAnalysis { get; set; }
    public string? DevResponse { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public string? UpdateBy { get; set; }
}
