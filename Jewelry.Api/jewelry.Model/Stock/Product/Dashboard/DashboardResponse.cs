using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Dashboard
{
    public class DashboardResponse
    {
        public StockSummary Summary { get; set; } = new StockSummary();
        public List<ProductCategoryBreakdown> Categories { get; set; } = new List<ProductCategoryBreakdown>();
        public List<LastActivity> LastActivities { get; set; } = new List<LastActivity>();
        public DateTime DataAtDate { get; set; } = DateTime.UtcNow;
    }

    public class StockSummary
    {
        public int TotalProducts { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal OnProcessQuantity { get; set; }
        public int AvailableCount { get; set; }
        public int OnProcessCount { get; set; }
    }

    public class ProductCategoryBreakdown
    {
        public string ProductTypeName { get; set; } = string.Empty;  // ประเภทสินค้า (แหวน, สร้อย, ต่างหู, etc.)
        public string ProductionType { get; set; } = string.Empty;    // สีของทอง/เงิน (White Gold, Yellow Gold, Rose Gold, etc.)
        public string ProductionTypeSize { get; set; } = string.Empty; // ประเภททอง/เงิน (18K, 14K, 925, etc.)
        public int Count { get; set; }                                 // จำนวนสินค้า
        public decimal TotalQuantity { get; set; }                     // จำนวนทั้งหมด
        public decimal TotalOnProcessQuantity { get; set; }            // จำนวนที่อยู่ระหว่างดำเนินการ
        public decimal TotalValue { get; set; }                        // มูลค่ารวม
        public decimal AveragePrice { get; set; }                      // ราคาเฉลี่ย
    }

    public class LastActivity
    {
        public string StockNumber { get; set; } = string.Empty;
        public string ProductNumber { get; set; } = string.Empty;
        public string ProductNameTh { get; set; } = string.Empty;
        public string ProductNameEn { get; set; } = string.Empty;
        public string ProductTypeName { get; set; } = string.Empty;
        public string ProductionType { get; set; } = string.Empty;
        public string ProductionTypeSize { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal ProductPrice { get; set; }
        public string Mold { get; set; } = string.Empty;
        public string WoText { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = string.Empty;
    }

    public class TodayReportResponse
    {
        public DateTime ReportDate { get; set; }
        public TodaySummary Summary { get; set; } = new TodaySummary();
        public List<LastActivity> Transactions { get; set; } = new List<LastActivity>();
    }

    public class TodaySummary
    {
        public int TotalTransactions { get; set; }
        public int NewStockItems { get; set; }
        public decimal TotalValue { get; set; }
        public int PriceChanges { get; set; }
        public int LowStockAlerts { get; set; }
    }

    public class WeeklyReportResponse
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public string WeekNumber { get; set; } = string.Empty;
        public WeeklySummary Summary { get; set; } = new WeeklySummary();
        public List<DailyMovement> DailyMovements { get; set; } = new List<DailyMovement>();
    }

    public class WeeklySummary
    {
        public int TotalTransactions { get; set; }
        public int NewStockItems { get; set; }
        public decimal TotalValue { get; set; }
        public int PriceChanges { get; set; }
        public int LowStockAlerts { get; set; }
    }

    public class DailyMovement
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public int NewStockCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class MonthlyReportResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public DateTime MonthStartDate { get; set; }
        public DateTime MonthEndDate { get; set; }
        public MonthlySummary Summary { get; set; } = new MonthlySummary();
        public List<WeeklyComparison> WeeklyComparisons { get; set; } = new List<WeeklyComparison>();
    }

    public class MonthlySummary
    {
        public int TotalTransactions { get; set; }
        public int NewStockItems { get; set; }
        public decimal TotalValue { get; set; }
        public int PriceChanges { get; set; }
        public int TotalAvailableProducts { get; set; }
    }

    public class WeeklyComparison
    {
        public int WeekNumber { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public int TransactionCount { get; set; }
        public int NewStockCount { get; set; }
        public decimal TotalValue { get; set; }
    }
}
