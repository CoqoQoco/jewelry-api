using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class DashboardResponse
    {
        public StockSummary Summary { get; set; } = new StockSummary();
        public List<GemCategoryBreakdown> Categories { get; set; } = new List<GemCategoryBreakdown>();
        public List<TransactionTrend> Trends { get; set; } = new List<TransactionTrend>();
        public List<TopGemMovement> TopMovements { get; set; } = new List<TopGemMovement>();
        public List<PriceChangeAlert> PriceAlerts { get; set; } = new List<PriceChangeAlert>();
    }

    public class StockSummary
    {
        public int TotalGemTypes { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalQuantityWeight { get; set; }
        public decimal TotalValue { get; set; }
        public decimal TotalOnProcessQuantity { get; set; }
        public decimal TotalOnProcessQuantityWeight { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal AvailableQuantityWeight { get; set; }
        public int LowStockCount { get; set; }
        public int ZeroStockCount { get; set; }
    }

    public class GemCategoryBreakdown
    {
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalQuantityWeight { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    public class TransactionTrend
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal TotalQuantityWeightIn { get; set; }
        public decimal TotalQuantityWeightOut { get; set; }
        public decimal NetQuantityChange { get; set; }
        public decimal NetQuantityWeightChange { get; set; }
    }

    public class TopGemMovement
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalQuantityMoved { get; set; }
        public decimal TotalQuantityWeightMoved { get; set; }
        public decimal CurrentQuantity { get; set; }
        public decimal CurrentQuantityWeight { get; set; }
        public decimal CurrentPrice { get; set; }
    }

    public class PriceChangeAlert
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal PreviousPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal ChangePercentage { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; } = string.Empty; // INCREASE, DECREASE
    }
}