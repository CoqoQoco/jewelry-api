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
        public DateTime? CreateDateStart { get; set; }
        public DateTime? CreateDateEnd { get; set; }
        public DateTime? QuotationDateStart { get; set; }
        public DateTime? QuotationDateEnd { get; set; }
        public string? Currency { get; set; }
        public string? CreateBy { get; set; }
    }
}