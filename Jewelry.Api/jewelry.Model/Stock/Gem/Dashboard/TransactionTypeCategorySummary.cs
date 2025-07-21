using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class TransactionTypeCategorySummary
    {
        public int Type { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
        
        // Gem breakdown within this transaction type
        public List<GemTransactionDetail> GemDetails { get; set; } = new List<GemTransactionDetail>();
        
        // Production plan details for Type 7 only
        public List<ProductionPlanUsage>? ProductionPlanUsages { get; set; }
    }

    public class GemTransactionDetail
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;

        public int TransactionCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal CurrentWeight { get; set; }
        public decimal AveragePrice { get; set; }
        public DateTime LastTransactionDate { get; set; }
        
        // For Type 7 transactions - production categorization (Gold/Silver)
        public string? ProductionType { get; set; } // e.g., "Gold", "Silver"
        public string? ProductionTypeName { get; set; } // e.g., "ทอง", "เงิน"
        
        // Additional production details for Type 7
        public List<ProductionDetail>? ProductionDetails { get; set; }
    }

    public class ProductionDetail
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
        public string JewelryType { get; set; } = string.Empty; // แหวน, สร้อยคอ, etc.
    }
    
}