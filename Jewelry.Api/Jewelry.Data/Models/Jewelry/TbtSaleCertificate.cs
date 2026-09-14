using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleCertificate
{
    public string Running { get; set; } = null!;

    public string Batch { get; set; } = null!;

    public string CertificateNo { get; set; } = null!;

    public int IssueNo { get; set; }

    public string? InvoiceRunning { get; set; }

    public string InvoiceNo { get; set; } = null!;

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
