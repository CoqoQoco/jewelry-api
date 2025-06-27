using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class WeeklyReportResponse
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public string WeekNumber { get; set; } = string.Empty;
        public WeeklyStockSummary Summary { get; set; } = new WeeklyStockSummary();
        public List<DailyMovement> DailyMovements { get; set; } = new List<DailyMovement>();
        public List<WeeklyTopMovement> TopMovements { get; set; } = new List<WeeklyTopMovement>();
        public List<WeeklyPerformance> Performance { get; set; } = new List<WeeklyPerformance>();
        public List<WeeklyTrendAnalysis> TrendAnalysis { get; set; } = new List<WeeklyTrendAnalysis>();
    }

    public class WeeklyStockSummary
    {
        public int TotalTransactions { get; set; }
        public int TotalPriceChanges { get; set; }
        public int NewStockItems { get; set; }
        public decimal WeekOpeningValue { get; set; }
        public decimal WeekClosingValue { get; set; }
        public decimal NetValueChange { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal TotalQuantityWeightIn { get; set; }
        public decimal TotalQuantityWeightOut { get; set; }
        public decimal AverageTransactionsPerDay { get; set; }
        public int PeakTransactionDay { get; set; }
        public int LowestTransactionDay { get; set; }
    }

    public class DailyMovement
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal TotalQuantityWeightIn { get; set; }
        public decimal TotalQuantityWeightOut { get; set; }
        public decimal NetQuantityChange { get; set; }
        public decimal NetQuantityWeightChange { get; set; }
        public int PriceChanges { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class WeeklyTopMovement
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalQuantityMoved { get; set; }
        public decimal TotalQuantityWeightMoved { get; set; }
        public decimal WeekStartQuantity { get; set; }
        public decimal WeekEndQuantity { get; set; }
        public decimal QuantityChange { get; set; }
        public string MovementType { get; set; } = string.Empty; // HIGH_IN, HIGH_OUT, HIGH_USAGE
    }

    public class WeeklyPerformance
    {
        public string GroupName { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public decimal QuantityTurnover { get; set; }
        public decimal QuantityWeightTurnover { get; set; }
        public int PriceChanges { get; set; }
        public decimal AveragePriceChange { get; set; }
    }

    public class WeeklyTrendAnalysis
    {
        public string Category { get; set; } = string.Empty; // GROUP, SHAPE, GRADE
        public string CategoryValue { get; set; } = string.Empty;
        public string TrendDirection { get; set; } = string.Empty; // UP, DOWN, STABLE
        public decimal ChangePercentage { get; set; }
        public string TrendIndicator { get; set; } = string.Empty; // INCREASING_DEMAND, DECREASING_DEMAND, STABLE_DEMAND
        public decimal WeekOverWeekChange { get; set; }
    }
}