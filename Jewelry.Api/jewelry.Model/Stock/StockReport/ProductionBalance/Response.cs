using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.ProductionBalance
{
    public class Response
    {
        public List<GroupData> Groups { get; set; } = new List<GroupData>();
        public decimal TotalStockQty { get; set; }
        public decimal TotalSoldQty { get; set; }
        public DateTimeOffset? SalesStart { get; set; }
        public DateTimeOffset? SalesEnd { get; set; }
    }

    public class GroupData
    {
        public string Key { get; set; } = null!;
        public string? Label { get; set; }
        public decimal StockQty { get; set; }
        public decimal StockPercent { get; set; }
        public decimal SoldQty { get; set; }
        public decimal SoldPercent { get; set; }
        public decimal? Index { get; set; }
        public string Status { get; set; } = null!;
    }
}
