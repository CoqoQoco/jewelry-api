namespace jewelry.Model.Sale.ExportShipment.AddItems;

public class Request
{
    public string Running { get; set; }
    public List<string>? StockNumbers { get; set; }
    public Filter? Filter { get; set; }
}

public class Filter
{
    public List<string>? LocationCodes { get; set; }
    public List<string>? ProductType { get; set; }
    public List<string>? ProductionType { get; set; }
    public List<string>? ProductionTypeSize { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Keyword { get; set; }
}
