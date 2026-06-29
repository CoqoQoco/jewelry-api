using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleInvoicePrintLog
{
    public string Running { get; set; } = null!;

    public string? InvoiceRunning { get; set; }

    public string InvoiceNo { get; set; } = null!;

    public string PaperType { get; set; } = null!;

    public int CopyNo { get; set; }

    public string? Data { get; set; }

    public string PrintedBy { get; set; } = null!;

    public DateTime PrintedAt { get; set; }
}
