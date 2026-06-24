using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtTicketLog
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string Action { get; set; } = null!;

    public string? Detail { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
