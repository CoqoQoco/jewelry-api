using Kendo.DynamicLinqCore;
using System;

namespace jewelry.Model.Sale.MaterialSale.List
{
    public class Request : DataSourceRequest
    {
        public string? DocumentNo { get; set; }
        public string? CustomerName { get; set; }
        public int[]? Status { get; set; }
        public DateTimeOffset? DocumentDateStart { get; set; }
        public DateTimeOffset? DocumentDateEnd { get; set; }
        public string? CreateBy { get; set; }
    }
}
