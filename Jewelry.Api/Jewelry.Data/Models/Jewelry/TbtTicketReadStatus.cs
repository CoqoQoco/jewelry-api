using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtTicketReadStatus
{
    public long Id { get; set; }

    public long TicketId { get; set; }

    public string Username { get; set; } = null!;

    public DateTime LastReadDate { get; set; }
}
