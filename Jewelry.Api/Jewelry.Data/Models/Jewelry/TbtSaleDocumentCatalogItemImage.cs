namespace Jewelry.Data.Models.Jewelry;

public partial class TbtSaleDocumentCatalogItemImage
{
    public long Id { get; set; }

    public long ItemId { get; set; }

    public string BlobPath { get; set; } = null!;

    public int SortOrder { get; set; }

    public virtual TbtSaleDocumentCatalogItem ItemNavigation { get; set; } = null!;
}
