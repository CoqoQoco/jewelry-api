using System;
using System.Collections.Generic;

namespace jewelry.Model.Certificate.List
{
    public class Item
    {
        public string Running { get; set; } = null!;
        public string Batch { get; set; } = null!;
        public string CertificateNo { get; set; } = null!;
        public int IssueNo { get; set; }
        public string? InvoiceNumber { get; set; }
        public string StockNumber { get; set; } = null!;
        public string? ItemNo { get; set; }
        public string? CustomerCode { get; set; }
        public string BrandMode { get; set; } = null!;
        public string? BrandName { get; set; }
        public string? BrandLogoPath { get; set; }
        public string? ImagePath { get; set; }
        public string? SignerTitle { get; set; }
        public string Data { get; set; } = null!;
        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
    }

    public class Response
    {
        public List<Item> Data { get; set; } = new();
        public int Total { get; set; }
    }
}
