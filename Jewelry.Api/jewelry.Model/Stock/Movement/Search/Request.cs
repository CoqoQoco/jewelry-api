using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Stock.Movement.Search
{
    public class Request : DataSourceRequest
    {
        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? StockNumber { get; set; }
        public string? CurrentLocation { get; set; }
        public string? MovedBy { get; set; }
        public string? StockNumberOrigin { get; set; }
    }
}
