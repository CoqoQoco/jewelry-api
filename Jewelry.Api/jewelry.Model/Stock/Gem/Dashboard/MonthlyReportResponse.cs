using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class MonthlyReportResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public DateTime MonthStartDate { get; set; }
        public DateTime MonthEndDate { get; set; }
        public MonthlyStockSummary Summary { get; set; } = new MonthlyStockSummary();
        public List<WeeklyComparison> WeeklyComparisons { get; set; } = new List<WeeklyComparison>();
        public List<MonthlyTopPerformer> TopPerformers { get; set; } = new List<MonthlyTopPerformer>();
        public List<MonthlyInventoryAnalysis> InventoryAnalysis { get; set; } = new List<MonthlyInventoryAnalysis>();
        public List<MonthlyPriceAnalysis> PriceAnalysis { get; set; } = new List<MonthlyPriceAnalysis>();
        public List<MonthlySupplierAnalysis> SupplierAnalysis { get; set; } = new List<MonthlySupplierAnalysis>();
    }

    public class MonthlyStockSummary
    {
        public int TotalTransactions { get; set; }
        public int TotalPriceChanges { get; set; }
        public int NewStockItems { get; set; }
        public decimal MonthOpeningValue { get; set; }
        public decimal MonthClosingValue { get; set; }
        public decimal NetValueChange { get; set; }
        public decimal MonthOverMonthGrowth { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal TotalQuantityWeightIn { get; set; }
        public decimal TotalQuantityWeightOut { get; set; }
        public decimal InventoryTurnoverRatio { get; set; }
        public decimal AverageTransactionsPerDay { get; set; }
        public int PeakTransactionWeek { get; set; }
        public int LowestTransactionWeek { get; set; }
        public decimal TotalSupplierCost { get; set; }
        public decimal AverageSupplierCost { get; set; }
    }

    public class WeeklyComparison
    {
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal QuantityWeightIn { get; set; }
        public decimal QuantityWeightOut { get; set; }
        public int PriceChanges { get; set; }
        public decimal WeekOverWeekChange { get; set; }
    }

    public class MonthlyTopPerformer
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string PerformanceType { get; set; } = string.Empty; // HIGHEST_VOLUME, HIGHEST_VALUE, MOST_ACTIVE, FASTEST_MOVING
        public int TransactionCount { get; set; }
        public decimal TotalQuantityMoved { get; set; }
        public decimal TotalQuantityWeightMoved { get; set; }
        public decimal TotalValue { get; set; }
        public decimal MonthStartQuantity { get; set; }
        public decimal MonthEndQuantity { get; set; }
        public decimal TurnoverRate { get; set; }
        public int Ranking { get; set; }
    }

    public class MonthlyInventoryAnalysis
    {
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalQuantityWeight { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageQuantityPerItem { get; set; }
        public decimal AveragePricePerUnit { get; set; }
        public decimal InventoryDays { get; set; } // How many days of inventory based on usage
        public string InventoryStatus { get; set; } = string.Empty; // OVERSTOCK, OPTIMAL, UNDERSTOCK
        public decimal RecommendedOrderQuantity { get; set; }
        public decimal MonthOverMonthChange { get; set; }
    }

    public class MonthlyPriceAnalysis
    {
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int PriceChangeCount { get; set; }
        public decimal AveragePriceStart { get; set; }
        public decimal AveragePriceEnd { get; set; }
        public decimal PriceVolatility { get; set; }
        public decimal MaxPriceIncrease { get; set; }
        public decimal MaxPriceDecrease { get; set; }
        public string PriceTrend { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE, VOLATILE
        public decimal StandardDeviation { get; set; }
        public DateTime MostRecentPriceChange { get; set; }
    }

    public class MonthlySupplierAnalysis
    {
        public string? SupplierName { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalQuantityWeight { get; set; }
        public decimal TotalCost { get; set; }
        public decimal AverageCostPerUnit { get; set; }
        public decimal AverageCostPerWeight { get; set; }
        public List<string> GemTypes { get; set; } = new List<string>();
        public string PreferredGemCategory { get; set; } = string.Empty;
        public decimal SupplierPerformanceScore { get; set; }
        public int DeliveryCount { get; set; }
        public string ReliabilityRating { get; set; } = string.Empty; // EXCELLENT, GOOD, AVERAGE, POOR
    }
}