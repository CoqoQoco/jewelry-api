using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Stock.Gold.Transection
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string[]? GoldCode { get; set; }
        public string[]? GoldSizeCode { get; set; }
        public int[]? Type { get; set; }

        public string? RefDocType { get; set; }
        public string? RefDocNo { get; set; }

        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
    }
}
