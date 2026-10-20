namespace jewelry.Model.Stock.Product.QuickPrice
{
    public class CreateRequest
    {
        public string StockNumber { get; set; } = null!;
        public decimal Markup { get; set; }
        public decimal GoldWeight { get; set; }
        public decimal GoldPrice { get; set; }
        public decimal Percent { get; set; }
        public int Rounding { get; set; }
        public string? Remark { get; set; }
    }
}
