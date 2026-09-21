using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.Invoice.Get
{
    public class Response
    {
        public string InvoiceNumber { get; set; }
        public string? DKInvoiceNumber { get; set; }
        public string SoNumber { get; set; }
        public string InvoiceType { get; set; } = "PRODUCT";

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
        // เลขผู้เสียภาษีของลูกค้า — มีเฉพาะใบแจ้งหนี้วัตถุดิบ (มาจาก SM) ใบแจ้งหนี้สินค้าเป็น null เสมอ
        public string? CustomerTaxId { get; set; }

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

        public string? SalePerson { get; set; }
        public string? SaleSupport { get; set; }

        public decimal SpecialDiscount { get; set; }
        public decimal SpecialAddition { get; set; }
        public decimal FreightAndInsurance { get; set; }
        public decimal Vat { get; set; }

        public decimal? SubTotal { get; set; }
        public decimal? SpecialDiscountAmt { get; set; }
        public decimal? SpecialAdditionAmt { get; set; }
        public decimal? FreightAmt { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? GrandTotalRaw { get; set; }
        public decimal? GrandTotalRounded { get; set; }
        public decimal? RoundingAdjustment { get; set; }

        public int Status { get; set; }
        public string StatusName { get; set; }


        // List of confirmed items with invoice info (like Sale Order's StockConfirm) — ใช้เฉพาะ InvoiceType PRODUCT
        public List<Item> ConfirmedItems { get; set; } = new List<Item>();
        public List<InvoicePaymentItem> Payments { get; set; } = new List<InvoicePaymentItem>();

        // ข้อมูลใบสั่งขายวัตถุดิบต้นทาง — มีค่าเฉพาะ InvoiceType MATERIAL
        public string? MaterialSaleRunning { get; set; }
        public string? MaterialSaleDocumentNo { get; set; }
        public DateTime? MaterialSaleDocumentDate { get; set; }
        public List<MaterialItem>? MaterialItems { get; set; }
    }

    public class MaterialItem
    {
        public int ItemNo { get; set; }
        public string GemCode { get; set; } = null!;
        public string? GemName { get; set; }
        public string? GemGroup { get; set; }
        public string? GemShape { get; set; }
        public string? GemSize { get; set; }
        public string? GemGrade { get; set; }
        public string? Description { get; set; }
        public decimal QtyPiece { get; set; }
        public decimal QtyWeight { get; set; }
        public decimal PriceInclVat { get; set; }
        public decimal PriceExclVat { get; set; }
        public decimal Amount { get; set; }
        public string? Remark { get; set; }
    }

    public class Item
    {
        public long Id { get; set; }
        public string StockNumber { get; set; } = null!;
        public string? LineKey { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsInvoice => !string.IsNullOrEmpty(Invoice);
        public string? Invoice { get; set; }
        public string? InvoiceItem { get; set; }
        public string? EarringStemSize { get; set; }
    }

    public class InvoicePaymentItem
    {
        public string Running { get; set; } = null!;

        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyUnit { get; set; } = null!;

        public string PaymentMethod { get; set; } = null!;
        public int Payment { get; set; }
        public string? BankCode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? Remark { get; set; }
        public string ImagePath { get; set; } = null!;

        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}