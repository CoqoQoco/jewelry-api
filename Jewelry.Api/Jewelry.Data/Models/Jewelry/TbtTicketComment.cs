using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtTicketComment
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string Type { get; set; } = null!;

    public string AuthorRole { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
