namespace jewelry.Model.Stock.Product.QuickPrice
{
    public class Response
    {
        public long Id { get; set; }
        public string StockNumber { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public decimal Markup { get; set; }
        public decimal GoldWeight { get; set; }
        public decimal GoldPrice { get; set; }
        public decimal Cost { get; set; }
        public decimal CostExGold { get; set; }
        public decimal Percent { get; set; }
        public int Rounding { get; set; }
        public decimal QuickPrice { get; set; }
        public string? Karat { get; set; }
        public decimal? AlloyFactor { get; set; }
        public decimal? AlloyWeight { get; set; }
        public decimal? AlloyPrice { get; set; }
        public decimal? GoldCostTotal { get; set; }
        public decimal? TotalPrice { get; set; }
        public decimal? GoldLossPercent { get; set; }
        public decimal? GoldLossCostTotal { get; set; }
        public decimal? GoldLossTotalPrice { get; set; }
        public string? Remark { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
    }
}
