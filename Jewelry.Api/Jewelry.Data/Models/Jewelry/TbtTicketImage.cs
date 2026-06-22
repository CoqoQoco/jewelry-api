using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtTicketImage
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public int Number { get; set; }

    public string Path { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtTicket Ticket { get; set; } = null!;
}
