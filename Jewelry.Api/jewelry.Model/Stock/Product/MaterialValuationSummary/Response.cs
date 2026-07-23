using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.MaterialValuationSummary
{
    public class Response
    {
        public SummaryItem Summary { get; set; } = new();
        public List<ByTypeItem> ByType { get; set; } = new();
    }

    public class SummaryItem
    {
        public int TotalCount { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class ByTypeItem
    {
        public string Type { get; set; } = null!;
        public int Count { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalValue { get; set; }
    }
}
