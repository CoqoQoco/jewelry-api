using System;
using System.ComponentModel.DataAnnotations;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Announcement;

public class FeedAnnouncementRequest
{
    public int Take { get; set; } = 5;
    public int Skip { get; set; } = 0;
}

public class GetAnnouncementRequest
{
    public long Id { get; set; }
}

public class SearchAnnouncementRequest : DataSourceRequest
{
    public string? Keyword { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsPinned { get; set; }
    public string? Audience { get; set; }
}

public class CreateAnnouncementRequest
{
    [Required]
    public string Title { get; set; } = null!;

    [Required]
    public string Body { get; set; } = null!;

    public bool IsPinned { get; set; }

    public DateTimeOffset? PublishStart { get; set; }

    public DateTimeOffset? PublishEnd { get; set; }

    public bool IsPublished { get; set; } = true;

    public string Audience { get; set; } = AnnouncementAudience.All;

    public IFormFile? Image { get; set; }
}

public class UpdateAnnouncementRequest : CreateAnnouncementRequest
{
    public long Id { get; set; }

    public bool RemoveImage { get; set; }
}

public class TogglePublishAnnouncementRequest
{
    public long Id { get; set; }
    public bool IsPublished { get; set; }
}

public class DeleteAnnouncementRequest
{
    public long Id { get; set; }
}
