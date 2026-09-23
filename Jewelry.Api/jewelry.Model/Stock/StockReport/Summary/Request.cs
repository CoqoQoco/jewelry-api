using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.Summary
{
    public class Request
    {
        public List<string>? LocationCodes { get; set; }
        public List<string>? ProductTypes { get; set; }
        public List<string>? Golds { get; set; }
        public List<string>? GoldSizes { get; set; }
        public DateTimeOffset? SalesStart { get; set; }
        public DateTimeOffset? SalesEnd { get; set; }
    }
}
