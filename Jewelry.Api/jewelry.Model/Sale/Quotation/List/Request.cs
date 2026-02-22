using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Sale.Quotation.List
{
    public class Request : DataSourceRequest
    {
       public Search Search { get; set; } 
    }

    public class Search
    {
        public string? Number { get; set; }
        public string? CustomerName { get; set; }
        public DateTimeOffset? CreateDateStart { get; set; }
        public DateTimeOffset? CreateDateEnd { get; set; }
        public DateTimeOffset? QuotationDateStart { get; set; }
        public DateTimeOffset? QuotationDateEnd { get; set; }
        public string? Currency { get; set; }
        public string? CreateBy { get; set; }
    }
}