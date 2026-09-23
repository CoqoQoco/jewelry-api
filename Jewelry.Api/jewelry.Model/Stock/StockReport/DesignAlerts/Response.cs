using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.DesignAlerts
{
    public class Response
    {
        public List<ItemData> Data { get; set; } = new List<ItemData>();
        public int Total { get; set; }
    }

    public class ItemData
    {
        public string Design { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductNameTh { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? ImageBlobPath { get; set; }
        public decimal SoldQty { get; set; }
        public decimal StockQty { get; set; }
        public DateTime? OldestProductionDate { get; set; }
        public int AgeDays { get; set; }
        public int SkuCount { get; set; }
    }
}
