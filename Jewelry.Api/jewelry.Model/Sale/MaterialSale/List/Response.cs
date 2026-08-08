using System;

namespace jewelry.Model.Sale.MaterialSale.List
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public string DocumentNo { get; set; } = null!;
        public DateTime DocumentDate { get; set; }
        public string? CustomerName { get; set; }

        public int ItemCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal GrandTotal { get; set; }

        public int Status { get; set; }
        public string? StatusName { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
    }
}
