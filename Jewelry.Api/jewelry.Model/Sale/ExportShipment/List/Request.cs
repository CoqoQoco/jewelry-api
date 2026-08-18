using Kendo.DynamicLinqCore;

namespace jewelry.Model.Sale.ExportShipment.List;

public class Request : DataSourceRequest
{
    public Search? Search { get; set; }
}

public class Search
{
    public string? Keyword { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public int? Status { get; set; }
}
