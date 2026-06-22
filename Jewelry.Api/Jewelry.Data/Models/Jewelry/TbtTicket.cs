using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtTicket
{
    public long Id { get; set; }

    public string TicketNo { get; set; } = null!;

    public int Type { get; set; }

    public string TopicRoute { get; set; } = null!;

    public string TopicName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? ScreenshotUrl { get; set; }

    public int Status { get; set; }

    public string? DevAnalysis { get; set; }

    public string? DevResponse { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmTicketStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<TbtTicketImage> TbtTicketImage { get; set; } = new List<TbtTicketImage>();
}
