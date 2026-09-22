using System;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtProductGallery
{
    public long Id { get; set; }

    public string ScopeType { get; set; } = null!;

    public string ScopeKey { get; set; } = null!;

    public string BlobPath { get; set; } = null!;

    public int SortOrder { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public long? SizeBytes { get; set; }

    public string? ContentType { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }
}
