using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.SalesSummary
{
    public class Request
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public List<string>? SaleChannelCodes { get; set; }
        public string? CustomerCode { get; set; }
    }
}
