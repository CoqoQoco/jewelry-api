using System;

namespace jewelry.Model.Sale.SaleReport.PipelineSummary
{
    public class Request
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}
