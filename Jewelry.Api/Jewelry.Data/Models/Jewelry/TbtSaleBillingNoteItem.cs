using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleBillingNoteItem
{
    public long Id { get; set; }

    public string BillingNoteRunning { get; set; } = null!;

    public int Seq { get; set; }

    public string InvoiceRunning { get; set; } = null!;

    public DateTime? InvoiceDate { get; set; }

    public decimal AmountBeforeVat { get; set; }

    public string? Remark { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual TbtSaleBillingNoteHeader BillingNoteRunningNavigation { get; set; } = null!;
}
