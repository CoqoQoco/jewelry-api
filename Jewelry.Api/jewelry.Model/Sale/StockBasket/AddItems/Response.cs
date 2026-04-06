namespace jewelry.Model.Sale.StockBasket.AddItems;

public class Response
{
    public int AddedCount { get; set; }
    public List<string> SkippedStockNumbers { get; set; } = new();
    public string? Message { get; set; }
}
