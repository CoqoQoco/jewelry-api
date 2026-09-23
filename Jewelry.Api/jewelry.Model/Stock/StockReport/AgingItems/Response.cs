using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.AgingItems
{
    public class Response
    {
        public List<ItemData> Data { get; set; } = new List<ItemData>();
        public int Total { get; set; }
    }

    public class ItemData
    {
        public string StockNumber { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }
        public string Design { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductNameTh { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? ImageBlobPath { get; set; }
        public string LocationCode { get; set; } = null!;
        public string? LocationName { get; set; }
        public DateTime? ProductionDate { get; set; }
        public int AgeDays { get; set; }
        public decimal Qty { get; set; }
    }
}
