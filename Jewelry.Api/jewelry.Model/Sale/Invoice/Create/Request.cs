using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.Invoice.Create
{
    public class Request
    {
        public string? DKInvoiceNumber { get; set; }
        public string SoNumber { get; set; }

        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerRemark { get; set; }

        public string CurrencyUnit { get; set; }
        public decimal CurrencyRate { get; set; }

        public DateTimeOffset? DeliveryDate { get; set; }
        public decimal Deposit { get; set; }

        public decimal GoldRate { get; set; }
        public decimal Markup { get; set; }

        public int Payment { get; set; }
        public string PaymentName { get; set; }
        public int PaymentDay { get; set; }

        public string Priority { get; set; }
        public string? RefQuotation { get; set; }
        public string? Remark { get; set; }
      

        public decimal SpecialDiscount { get; set; }
        public decimal SpecialAddition { get; set; }
        public decimal FreightAndInsurance { get; set; }
        public decimal Vat { get; set; }

        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }

    public class InvoiceItem
    {
        public string StockNumber { get; set; } = null!;
       
    }
}