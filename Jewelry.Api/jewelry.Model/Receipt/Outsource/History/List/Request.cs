using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Receipt.Outsource.History.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public DateTimeOffset? ReceiptDateStart { get; set; }
        public DateTimeOffset? ReceiptDateEnd { get; set; }

        public string? StockNumber { get; set; }
        public string? Mold { get; set; }

        public string? ProductNumber { get; set; }
        public string? ProductNameEn { get; set; }
        public string? ProductNameTh { get; set; }

        public string[]? ProductType { get; set; }
        public string? Size { get; set; }

        public string? Vendor { get; set; }
        public string? PoNumber { get; set; }
    }
}
