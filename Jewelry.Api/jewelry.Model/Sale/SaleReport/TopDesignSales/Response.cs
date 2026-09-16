using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleReport.TopDesignSales
{
    public class Response
    {
        public List<DesignData> Designs { get; set; } = new List<DesignData>();
        public int TotalDesignCount { get; set; }
        public decimal TotalPieceCount { get; set; }
        public decimal TotalAmountThb { get; set; }
    }

    public class DesignData
    {
        public string DesignKey { get; set; } = null!;
        public string? ProductTypeName { get; set; }
        public string? ImageBlobPath { get; set; }
        public decimal PieceCount { get; set; }
        public decimal AmountThb { get; set; }
        public List<string> MoldCodes { get; set; } = new List<string>();
        public List<string> Golds { get; set; } = new List<string>();
        public List<ItemData> Items { get; set; } = new List<ItemData>();
    }

    public class ItemData
    {
        public string StockNumber { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }
        public string SkuCode { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? Mold { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? Size { get; set; }
        public string? ImageBlobPath { get; set; }
        public decimal PieceCount { get; set; }
        public decimal AmountThb { get; set; }
    }
}
