using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Sale.SaleOrder.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string? SoNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? RefQuotation { get; set; }
        public string? CurrencyUnit { get; set; }
        public int? Status { get; set; }
        public string? CreateBy { get; set; }
        
        public DateTimeOffset? CreateDateStart { get; set; }
        public DateTimeOffset? CreateDateEnd { get; set; }
        
        public DateTimeOffset? DeliveryDateStart { get; set; }
        public DateTimeOffset? DeliveryDateEnd { get; set; }
    }
}