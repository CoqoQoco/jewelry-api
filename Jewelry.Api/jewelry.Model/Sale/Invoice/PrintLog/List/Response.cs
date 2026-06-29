using System;

namespace jewelry.Model.Sale.Invoice.PrintLog.List
{
    public class Response
    {
        public string Running { get; set; } = null!;
        public string InvoiceNo { get; set; } = null!;
        public string PaperType { get; set; } = null!;
        public int CopyNo { get; set; }
        public string? Data { get; set; }
        public string PrintedBy { get; set; } = null!;
        public DateTime PrintedAt { get; set; }
    }
}
