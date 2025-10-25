using System;

namespace jewelry.Model.Sale.InvoiceVersion.Get
{
    public class Response
    {
        public string VersionNumber { get; set; } = null!;
        public string InvoiceNumber { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public string Data { get; set; } = null!;

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public bool? IsActive { get; set; }
    }
}
