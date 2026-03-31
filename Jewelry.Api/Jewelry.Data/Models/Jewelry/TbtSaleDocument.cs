using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleDocument
{
    public int Id { get; set; }

    public string FileName { get; set; } = null!;

    public string BlobPath { get; set; } = null!;

    public int DocumentMonth { get; set; }

    public int DocumentYear { get; set; }

    public string? Tags { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }
}
