using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.List
{
    public class Request : DataSourceRequest
    {
      public RequestSearch Search { get; set;  }
    }

    public class RequestSearch
    {
        public DateTimeOffset? RecieptStart { get; set; }
        public DateTimeOffset? ReceiptEnd { get; set; }

        public string? ReceiptNumber { get; set; }
        public string? StockNumber { get; set; }

        public string? Mold { get; set; }
        public string? WoText { get; set; }

        public string? ProductNumber { get; set; }
        public string[]? ProductType { get; set; }

        public string[]? Gold { get; set; }
        public string[]? GoldSize { get; set; }
    }
}
