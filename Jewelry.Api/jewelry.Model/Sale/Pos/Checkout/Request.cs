using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.Pos.Checkout
{
    public class Request
    {
        public string IdempotencyKey { get; set; } = null!;

        public string CustomerCode { get; set; } = null!;
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerRemark { get; set; }

        public string CurrencyUnit { get; set; } = "THB";
        public decimal CurrencyRate { get; set; } = 1;
        public decimal SpecialDiscount { get; set; }
        public decimal SpecialAddition { get; set; }
        public decimal FreightAndInsurance { get; set; }
        public decimal Vat { get; set; }

        public List<CheckoutItem> Items { get; set; } = new List<CheckoutItem>();
        public List<CheckoutPayment> Payments { get; set; } = new List<CheckoutPayment>();

        public string? DkInvoiceNumber { get; set; }
        public string? Remark { get; set; }
        public int? PaymentDay { get; set; }
        public decimal? Deposit { get; set; }
    }

    public class CheckoutItem
    {
        public string StockNumber { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public decimal AppraisalPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class CheckoutPayment
    {
        public int Payment { get; set; }
        public string PaymentName { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTimeOffset PaymentDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? BankCode { get; set; }
        public string? BankBranch { get; set; }
        public string? Remark { get; set; }
    }
}
