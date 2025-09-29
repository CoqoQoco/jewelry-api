using System;

namespace jewelry.Model.Sale.Quotation.List
{
    public class Response
    {
        public string Number { get; set; } = string.Empty;
        public string Running { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal CurrencyRate { get; set; }
        public decimal MarkUp { get; set; }
        public decimal Discount { get; set; }
        public decimal? Freight { get; set; }
        public string Remark { get; set; } = string.Empty;
        public DateTime? Date { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
        public DateTime? UpdateDate { get; set; }
        public string UpdateBy { get; set; } = string.Empty;
    }
}