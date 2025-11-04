using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.Invoice.Get
{
    public class Response
    {
        public string InvoiceNumber { get; set; }
        public string SoNumber { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerRemark { get; set; }

        public string CurrencyUnit { get; set; }
        public decimal CurrencyRate { get; set; }

        public DateTime? DeliveryDate { get; set; }
        public decimal Deposit { get; set; }

        public decimal? GoldRate { get; set; }
        public decimal? Markup { get; set; }

        public int Payment { get; set; }
        public string PaymentName { get; set; }
        public int PaymentDay { get; set; }

        public string? Priority { get; set; }
        public string? RefQuotation { get; set; }
        public string? Remark { get; set; }


        public decimal SpecialDiscount { get; set; }
        public decimal SpecialAddition { get; set; }
        public decimal FreightAndInsurance { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }


        // List of confirmed items with invoice info (like Sale Order's StockConfirm)
        public List<Item> ConfirmedItems { get; set; } = new List<Item>();
        public List<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class Item
    {
        public long Id { get; set; }
        public string StockNumber { get; set; } = null!;
        public bool IsConfirmed { get; set; }
        public bool IsInvoice => !string.IsNullOrEmpty(Invoice);
        public string? Invoice { get; set; }
        public string? InvoiceItem { get; set; }
    }

    public class Payment
    {
        public string Running { get; set; } = null!;

        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyUnit { get; set; } = null!;

        public string PaymentMethod { get; set; } = null!;
        public string? ReferenceNumber { get; set; }
        public string? Remark { get; set; }
        public string ImagePath { get; set; } = null!;

        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}