using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtPosCheckout
{
    public string IdempotencyKey { get; set; } = null!;

    public string SoNumber { get; set; } = null!;

    public string InvoiceNumber { get; set; } = null!;

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;
}
