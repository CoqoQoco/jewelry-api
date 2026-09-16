using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleOrderDeposit
{
    public string Running { get; set; } = null!;

    public string SoNumber { get; set; } = null!;

    public DateTime DepositDate { get; set; }

    public decimal Amount { get; set; }

    public string? CurrencyUnit { get; set; }

    public decimal? CurrencyRate { get; set; }

    public int? Payment { get; set; }

    public string? PaymentName { get; set; }

    public string? BankCode { get; set; }

    public string? BankBranch { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? ImagePath { get; set; }

    public string? Remark { get; set; }

    public bool IsDelete { get; set; }

    public string? DeleteReason { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtSaleOrderDepositApply> TbtSaleOrderDepositApply { get; set; } = new List<TbtSaleOrderDepositApply>();
}
