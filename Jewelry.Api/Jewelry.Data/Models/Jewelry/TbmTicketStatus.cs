using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmTicketStatus
{
    public int Id { get; set; }

    public string NameTh { get; set; } = null!;

    public string NameEn { get; set; } = null!;

    public virtual ICollection<TbtTicket> TbtTicket { get; set; } = new List<TbtTicket>();
}
