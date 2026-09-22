using System.Collections.Generic;

namespace jewelry.Model.Stock.StockProductGallery.Reorder;

public class Request
{
    public string StockNumber { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public List<long> Ids { get; set; } = new();
}
