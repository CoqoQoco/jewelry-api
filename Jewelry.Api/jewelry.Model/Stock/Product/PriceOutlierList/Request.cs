using Kendo.DynamicLinqCore;

namespace jewelry.Model.Stock.Product.PriceOutlierList
{
    public class Request : DataSourceRequest
    {
        public Search? Search { get; set; }
    }

    public class Search
    {
        public string? Text { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReviewStatus { get; set; }
        public jewelry.Model.Stock.Product.List.Search? Filter { get; set; }
    }
}
