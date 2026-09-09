using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtAnnouncement
{
    public long Id { get; set; }

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string? ImagePath { get; set; }

    public bool IsPinned { get; set; }

    public DateTime PublishStart { get; set; }

    public DateTime? PublishEnd { get; set; }

    public bool IsPublished { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public string Audience { get; set; } = "all";
}
