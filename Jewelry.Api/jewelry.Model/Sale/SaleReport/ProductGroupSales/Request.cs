using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.ProductGroupSales
{
    public class Request
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public List<string>? SaleChannelCodes { get; set; }
        public string? CustomerCode { get; set; }
        public string? GroupBy { get; set; }
        public List<string>? ProductTypes { get; set; }
        public List<string>? Golds { get; set; }
        public List<string>? GoldSizes { get; set; }
    }
}
