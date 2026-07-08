using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleBillingNoteProduct
{
    public long Id { get; set; }

    public string BillingNoteRunning { get; set; } = null!;

    public string InvoiceRunning { get; set; } = null!;

    public string? ProductNumber { get; set; }

    public string? ProductType { get; set; }

    public string? ProductTypeName { get; set; }

    public string? ProductionType { get; set; }

    public decimal Qty { get; set; }

    public decimal Amount { get; set; }

    public string? Remark { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual TbtSaleBillingNoteHeader BillingNoteRunningNavigation { get; set; } = null!;
}
