using System;
using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbmProductCatalog
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string NameTh { get; set; } = null!;

    public string? NameEn { get; set; }

    public string? CollectionTitle { get; set; }

    public string? HeaderLabel { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreateDate { get; set; }

    public string CreateBy { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public string? UpdateBy { get; set; }

    public virtual ICollection<TbtCatalogProduct> TbtCatalogProduct { get; set; } = new List<TbtCatalogProduct>();
}
