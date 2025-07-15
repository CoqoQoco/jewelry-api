using System;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class MonthlyGemTransactionSummary
    {
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        
        // Transaction counts
        public int TotalTransactions { get; set; }
        public int InboundTransactions { get; set; }
        public int OutboundTransactions { get; set; }
        
        // Quantity totals
        public decimal TotalQuantityUsed { get; set; }
        public decimal InboundQuantity { get; set; }
        public decimal OutboundQuantity { get; set; }
        
        // Weight totals
        public decimal TotalWeightUsed { get; set; }
        public decimal InboundWeight { get; set; }
        public decimal OutboundWeight { get; set; }
        
        // Value metrics
        public decimal AveragePrice { get; set; }
        public decimal TotalValue { get; set; }
        
        // Current stock levels
        public decimal CurrentQuantity { get; set; }
        public decimal CurrentWeight { get; set; }
        
        // Activity metrics
        public string MostActiveGemCode { get; set; } = string.Empty;
        public DateTime LastTransactionDate { get; set; }
    }
}