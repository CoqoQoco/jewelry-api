using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class TodayReportResponse
    {
        public DateTime ReportDate { get; set; }
        public TodayStockSummary Summary { get; set; } = new TodayStockSummary();
        public List<TodayTransaction> Transactions { get; set; } = new List<TodayTransaction>();
        public List<TodayPriceChange> PriceChanges { get; set; } = new List<TodayPriceChange>();
        public List<TodayNewStock> NewStocks { get; set; } = new List<TodayNewStock>();
        public List<TodayLowStock> LowStocks { get; set; } = new List<TodayLowStock>();
    }

    public class TodayStockSummary
    {
        public int TotalTransactions { get; set; }
        public int PriceChanges { get; set; }
        public int NewStockItems { get; set; }
        public int LowStockAlerts { get; set; }
        public decimal TotalValueIn { get; set; }
        public decimal TotalValueOut { get; set; }
        public decimal NetValueChange { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal TotalQuantityWeightIn { get; set; }
        public decimal TotalQuantityWeightOut { get; set; }
    }

    public class TodayTransaction
    {
        public string Running { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int Type { get; set; }
        public string TypeName { get; set; } = string.Empty; // IN, OUT, TRANSFER, etc.
        public decimal Qty { get; set; }
        public decimal QtyWeight { get; set; }
        public string? JobOrPo { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
    }

    public class TodayPriceChange
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public decimal PreviousPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercentage { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeBy { get; set; } = string.Empty;
    }

    public class TodayNewStock 
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal QuantityWeight { get; set; }
        public decimal Price { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
    }

    public class TodayLowStock
    {
        public string Code { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal CurrentQuantity { get; set; }
        public decimal CurrentQuantityWeight { get; set; }
        public decimal MinimumLevel { get; set; } // This would need to be added to database if not exists
        public string AlertLevel { get; set; } = string.Empty; // LOW, CRITICAL, ZERO
    }
}