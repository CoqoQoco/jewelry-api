using System;

namespace jewelry.Model.Sale.InvoicePayment.List
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public string InvoiceNumber { get; set; } = null!;
        public string SoNumber { get; set; } = null!;

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
