namespace jewelry.Model.Stock.Product.QuickPrice
{
    public class CreateRequest
    {
        public string StockNumber { get; set; } = null!;
        public decimal Markup { get; set; }
        public decimal GoldWeight { get; set; }
        public decimal GoldPrice { get; set; }
        public string? Karat { get; set; }
        public decimal AlloyFactor { get; set; }
        public decimal AlloyWeight { get; set; }
        public decimal AlloyPrice { get; set; }
        public decimal Percent { get; set; }
        public decimal? GoldLossPercent { get; set; }
        public int Rounding { get; set; }
        public string? Remark { get; set; }
    }
}
