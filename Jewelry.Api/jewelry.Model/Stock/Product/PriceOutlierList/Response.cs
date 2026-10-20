using jewelry.Model.Stock.Product.PriceCheck;

namespace jewelry.Model.Stock.Product.PriceOutlierList
{
    public class OutlierItem
    {
        public string StockNumber { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }
        public string SkuCode { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductNameTh { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public decimal Price { get; set; }
        public string? ImagePath { get; set; }
        public double Score { get; set; }
        public List<Reason> Reasons { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
        public decimal? AckPrice { get; set; }
        public string? AckBy { get; set; }
        public DateTimeOffset? AckDate { get; set; }
        public string? AckNote { get; set; }
        public bool IsAcked { get; set; }
        public bool PriceChangedSinceAck { get; set; }
    }
}
