using System;
using System.Collections.Generic;

namespace jewelry.Model.Announcement;

public class AnnouncementItemResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public bool IsPinned { get; set; }
    public DateTime PublishStart { get; set; }
    public DateTime? PublishEnd { get; set; }
    public bool IsPublished { get; set; }
    public string DisplayStatus { get; set; } = null!;
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; } = null!;
    public DateTime? UpdateDate { get; set; }
    public string? UpdateBy { get; set; }
}

public class FeedAnnouncementResponse
{
    public List<AnnouncementItemResponse> Data { get; set; } = new();
    public int Total { get; set; }
}

public class CreateAnnouncementResponse
{
    public long Id { get; set; }
}
