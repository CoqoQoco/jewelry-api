using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtCatalogProduct
{
    public long Id { get; set; }

    public int CatalogId { get; set; }

    public string ProductNumber { get; set; } = null!;

    public int SortOrder { get; set; }

    public string? Dimension1 { get; set; }

    public string? Dimension2 { get; set; }

    public string? Dimension3 { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual TbmProductCatalog Catalog { get; set; } = null!;
}
