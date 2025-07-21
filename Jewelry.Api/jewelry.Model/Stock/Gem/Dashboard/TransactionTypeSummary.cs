using System;
using System.Collections.Generic;

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
        
        // Additional properties for Type 7 (เบิกออกคลัง) with production plan details
        public List<ProductionPlanUsage>? ProductionPlanUsages { get; set; }
    }

    public class ProductionPlanUsage
    {
        public string Wo { get; set; } = string.Empty;
        public int WoNumber { get; set; }
        public string Mold { get; set; } = string.Empty;
        public string ProductNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string CustomerNumber { get; set; } = string.Empty;
        public decimal QuantityUsed { get; set; }
        public decimal WeightUsed { get; set; }
        public DateTime? RequestDate { get; set; }
        
        // Production categorization
        public string ProductionType { get; set; } = string.Empty;
        public string ProductionTypeSize { get; set; } = string.Empty;
    }
}