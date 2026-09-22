using Kendo.DynamicLinqCore;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockProductGallery.MissingList;

public class Request : DataSourceRequest
{
    public Search? Search { get; set; }
}

public class Search
{
    public string? Text { get; set; }
    public List<string>? ProductTypes { get; set; }
}
