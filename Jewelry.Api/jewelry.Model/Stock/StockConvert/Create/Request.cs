namespace jewelry.Model.Stock.StockConvert.Create;

public class Request
{
    public string? SoNumber { get; set; }
    public string? SoLineKey { get; set; }
    public List<string> SourceStockNumbers { get; set; } = new();
    public string? Remark { get; set; }
}
