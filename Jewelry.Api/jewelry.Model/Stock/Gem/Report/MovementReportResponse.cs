using System;

namespace jewelry.Model.Stock.Gem.Report
{
    public class MovementReportResponse
    {
        public string Code { get; set; }
        public string? GroupName { get; set; }
        public string? Shape { get; set; }
        public string? Grade { get; set; }
        public string? Size { get; set; }
        public decimal Quantity { get; set; }
        public decimal QuantityWeight { get; set; }
        public int TransactionCount { get; set; }
        public decimal QuantityIn { get; set; }
        public decimal QuantityWeightIn { get; set; }
        public decimal QuantityOut { get; set; }
        public decimal QuantityWeightOut { get; set; }
        public DateTime? LastMovementDate { get; set; }
        public int? DaysSinceLastMovement { get; set; }
        public decimal AvgDailyConsumption { get; set; }
        public decimal? DaysOfSupply { get; set; }
        public string MovementStatus { get; set; }
        public string StockAlertLevel { get; set; }
        public decimal Price { get; set; }
        public decimal PriceQty { get; set; }
    }
}
