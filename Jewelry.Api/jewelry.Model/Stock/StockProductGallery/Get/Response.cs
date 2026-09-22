using System.Collections.Generic;

namespace jewelry.Model.Stock.StockProductGallery.Get;

public class Response
{
    public string StockNumber { get; set; } = null!;
    public string? StockNumberOrigin { get; set; }
    public string SkuCode { get; set; } = null!;
    public string? Mold { get; set; }
    public int MoldPieceCount { get; set; }
    public bool CanUseMoldScope { get; set; }
    public List<jewelry.Model.Stock.StockProductGallery.Item> SkuImages { get; set; } = new();
    public List<jewelry.Model.Stock.StockProductGallery.Item> MoldImages { get; set; } = new();
    public List<string> Images { get; set; } = new();
}
