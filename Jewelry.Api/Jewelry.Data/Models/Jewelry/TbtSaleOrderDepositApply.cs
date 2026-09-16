using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleOrderDepositApply
{
    public long Id { get; set; }

    public string DepositRunning { get; set; } = null!;

    public string SoNumber { get; set; } = null!;

    public string InvoiceRunning { get; set; } = null!;

    public decimal Amount { get; set; }

    public bool IsDelete { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbtSaleOrderDeposit DepositRunningNavigation { get; set; } = null!;

    public virtual TbtSaleInvoiceHeader TbtSaleInvoiceHeader { get; set; } = null!;
}
