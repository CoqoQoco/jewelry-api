using System;

namespace jewelry.Model.Stock.StockProductGallery;

public class Item
{
    public long Id { get; set; }
    public string BlobPath { get; set; } = null!;
    public int SortOrder { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreateDate { get; set; }
    public string CreateBy { get; set; } = null!;
}
