using System;

namespace jewelry.Model.Sale.Invoice.List
{
    public class Response
    {
        public string InvoiceNumber { get; set; } = null!;
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
        public int? DepositPercent { get; set; }
        public decimal? Discount { get; set; }
        public decimal? GoldRate { get; set; }
        public decimal? Markup { get; set; }
        public string? PaymentName { get; set; }
        public int Payment { get; set; }
        public string Priority { get; set; } = null!;
        public string? RefQuotation { get; set; }
        public string? Remark { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = null!;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}