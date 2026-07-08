using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Sale.BillingNote.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; } = new Search();
    }

    public class Search
    {
        public string? Running { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerCode { get; set; }
        public int? Status { get; set; }
        public string? CreateBy { get; set; }

        public DateTimeOffset? DocumentDateFrom { get; set; }
        public DateTimeOffset? DocumentDateTo { get; set; }
    }
}
