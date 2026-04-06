namespace jewelry.Model.Sale.StockBasket.AddItems;

public class Request
{
    public string BasketRunning { get; set; }
    public List<string>? StockNumbers { get; set; }
    public CategoryFilter? CategoryFilter { get; set; }
}

public class CategoryFilter
{
    public string? ProductType { get; set; }
    public string? ProductionType { get; set; }
    public string? ProductionTypeSize { get; set; }
    public string? ReceiptNumber { get; set; }
}
