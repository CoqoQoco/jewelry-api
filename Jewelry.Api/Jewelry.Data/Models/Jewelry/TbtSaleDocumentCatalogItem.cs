using System.Collections.Generic;

namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleDocumentCatalogItem
{
    public long Id { get; set; }

    public long CatalogId { get; set; }

    public string? ProductNumber { get; set; }

    public string? DescriptionLine1 { get; set; }

    public string? DescriptionLine2 { get; set; }

    public string? Dimension1 { get; set; }

    public string? Dimension2 { get; set; }

    public string? Dimension3 { get; set; }

    public int SortOrder { get; set; }

    public virtual TbtSaleDocumentCatalog CatalogNavigation { get; set; } = null!;

    public virtual ICollection<TbtSaleDocumentCatalogItemImage> TbtSaleDocumentCatalogItemImage { get; set; } = new List<TbtSaleDocumentCatalogItemImage>();
}
