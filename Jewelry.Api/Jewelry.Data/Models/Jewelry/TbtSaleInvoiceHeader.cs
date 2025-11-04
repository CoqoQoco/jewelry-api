using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleInvoiceHeader
{
    public string Running { get; set; } = null!;

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public decimal CurrencyRate { get; set; }

    public string CurrencyUnit { get; set; } = null!;

    public string? CustomerAddress { get; set; }

    public string CustomerCode { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string CustomerName { get; set; } = null!;

    public string? CustomerRemark { get; set; }

    public string? CustomerTel { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public decimal Deposit { get; set; }

    public decimal GoldRate { get; set; }

    public decimal Markup { get; set; }

    public string PaymantName { get; set; } = null!;

    public int Payment { get; set; }

    public string Priority { get; set; } = null!;

    public string? RefQuotation { get; set; }

    public string? Remark { get; set; }

    public int Status { get; set; }

    public string StatusName { get; set; } = null!;

    public string SoRunning { get; set; } = null!;

    public decimal SpecialDiscount { get; set; }

    public decimal SpecialAddition { get; set; }

    public decimal FreightAndInsurance { get; set; }

    public int PaymentDay { get; set; }

    public bool IsDelete { get; set; }

    public string? DeleteReason { get; set; }

    public virtual ICollection<TbtSaleInvoicePaymentItem> TbtSaleInvoicePaymentItem { get; set; } = new List<TbtSaleInvoicePaymentItem>();
}
