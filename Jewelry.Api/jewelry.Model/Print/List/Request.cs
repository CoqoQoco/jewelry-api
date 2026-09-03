using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Print.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public DateTimeOffset? DateFrom { get; set; }
        public DateTimeOffset? DateTo { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? CreateBy { get; set; }
        public string? Status { get; set; }
    }
}
