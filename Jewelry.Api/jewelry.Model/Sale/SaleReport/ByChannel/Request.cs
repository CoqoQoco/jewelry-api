using System;

namespace jewelry.Model.Sale.SaleReport.ByChannel
{
    public class Request
    {
        public DateTimeOffset DateFrom { get; set; }
        public DateTimeOffset DateTo { get; set; }
        public string? SaleChannelCode { get; set; }
    }
}
