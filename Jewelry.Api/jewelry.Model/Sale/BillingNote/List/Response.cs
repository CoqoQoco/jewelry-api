using System;

namespace jewelry.Model.Sale.BillingNote.List
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public DateTime DocumentDate { get; set; }

        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;

        public int BillCount { get; set; }

        public decimal SubTotal { get; set; }
        public decimal VatPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrandTotal { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
