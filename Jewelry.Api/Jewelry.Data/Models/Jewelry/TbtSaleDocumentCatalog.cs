using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleDocumentCatalog
{
    public long Id { get; set; }

    public string? HeaderLabel { get; set; }

    public string? CollectionTitle { get; set; }

    public int? DocumentMonth { get; set; }

    public int? DocumentYear { get; set; }

    public string? Tags { get; set; }

    public string? Remark { get; set; }

    public int Status { get; set; }

    public string? StatusName { get; set; }

    public bool IsActive { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime CreateDate { get; set; }

    public string? UpdateBy { get; set; }

    public DateTime? UpdateDate { get; set; }

    public virtual ICollection<TbtSaleDocumentCatalogItem> TbtSaleDocumentCatalogItem { get; set; } = new List<TbtSaleDocumentCatalogItem>();
}
