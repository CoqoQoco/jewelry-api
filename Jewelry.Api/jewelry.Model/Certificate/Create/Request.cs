using System.Collections.Generic;

namespace jewelry.Model.Certificate.Create
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public string? CustomerCode { get; set; }
        public string? SignerTitle { get; set; }
        public BrandInfo Brand { get; set; } = null!;
        public bool SaveBrandAsCustomerDefault { get; set; }
        public List<CertificateItem> Certificates { get; set; } = new();
    }

    public class BrandInfo
    {
        public string Mode { get; set; } = null!;
        public string? Name { get; set; }
        public string? LogoPath { get; set; }
        public bool ShowManufacturer { get; set; }
        public bool ShowQr { get; set; }
    }

    public class CertificateItem
    {
        public string CertificateNo { get; set; } = null!;
        public string StockNumber { get; set; } = null!;
        public string? ItemNo { get; set; }
        public string? ImagePath { get; set; }
        public string Data { get; set; } = null!;
    }
}
