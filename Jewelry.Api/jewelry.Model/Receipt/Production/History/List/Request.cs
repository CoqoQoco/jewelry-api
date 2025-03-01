using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.History.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public DateTimeOffset ReceiptDateStart { get; set; }
        public DateTimeOffset ReceiptDateEnd { get; set; }
        public string[]? ReceiptType { get; set; }

        public string? StockNumber { get; set; }
        public string? Mold { get; set; }

        public string? ProductNumber { get; set; }
        public string? ProductNameEn { get; set; }
        public string? ProductNameTh { get; set; }

        public string? WoText { get; set; }
        public string[]? ProductType { get; set; }
        public string? Size { get; set; }

    }
}
