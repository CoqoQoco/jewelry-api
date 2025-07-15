using System;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class TransactionTypeSummary
    {
        public int Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
    }
}