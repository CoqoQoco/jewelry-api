using Kendo.DynamicLinqCore;

namespace jewelry.Model.Stock.StockConvert.List;

public class Request : DataSourceRequest
{
    public string? DocumentNumber { get; set; }
    public string? SoNumber { get; set; }
    public int? Status { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public string? SourceStockNumber { get; set; }
}
