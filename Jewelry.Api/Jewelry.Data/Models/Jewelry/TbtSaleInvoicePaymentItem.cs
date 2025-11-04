using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleInvoicePaymentItem
{
    public string Running { get; set; } = null!;

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public string CurrencyUnit { get; set; } = null!;

    public decimal Amount { get; set; }

    public string SoRunning { get; set; } = null!;

    public string InvoiceRunning { get; set; } = null!;

    public string ImagePath { get; set; } = null!;

    public DateTime PaymentDate { get; set; }

    public string? ReferenceNumber1 { get; set; }

    public string? Remark { get; set; }

    public bool IsDelete { get; set; }

    public string PaymantName { get; set; } = null!;

    public int Payment { get; set; }

    public virtual TbtSaleInvoiceHeader TbtSaleInvoiceHeader { get; set; } = null!;
}
