using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.Invoice.Create
{
    public class Request
    {
        public string SoNumber { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerRemark { get; set; }
        public string CurrencyUnit { get; set; } = null!;
        public decimal CurrencyRate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public int? DepositPercent { get; set; }
        public decimal? Discount { get; set; }
        public decimal? GoldRate { get; set; }
        public decimal? Markup { get; set; }
        public string? PaymentName { get; set; }
        public int Payment { get; set; }
        public string Priority { get; set; } = null!;
        public string? RefQuotation { get; set; }
        public string? Remark { get; set; }
        //public int Status { get; set; }
        //public string StatusName { get; set; } = null!;
        public string? Data { get; set; }
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        public string StockNumber { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }
        public long Id { get; set; }
        public decimal PriceOrigin { get; set; }
        public string CurrencyUnit { get; set; } = null!;
        public decimal CurrencyRate { get; set; }
        public decimal? Markup { get; set; }
        public decimal? Discount { get; set; }
        public decimal? GoldRate { get; set; }
        public string? Remark { get; set; }
        public string? NetPrice { get; set; }
        public decimal PriceDiscount { get; set; }
        public decimal PriceAfterCurrencyRate { get; set; }
        public decimal Qty { get; set; }
    }
}