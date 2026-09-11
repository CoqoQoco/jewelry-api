using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtNotification
{
    public long Id { get; set; }

    public string TypeCode { get; set; } = null!;

    public string RecipientUsername { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string? RefDocType { get; set; }

    public string? RefDocNo { get; set; }

    public string? ActionUrl { get; set; }

    public string Severity { get; set; } = null!;

    public decimal? Amount { get; set; }

    public string? CurrencyUnit { get; set; }

    public DateTime EventDate { get; set; }

    public DateTime? DueDate { get; set; }

    public string State { get; set; } = null!;

    public DateTime? SnoozeUntil { get; set; }

    public string Source { get; set; } = null!;

    public bool IsEscalated { get; set; }

    public DateTime? ReadDate { get; set; }

    public DateTime? DoneDate { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmNotificationType TbmNotificationType { get; set; } = null!;
}
