namespace jewelry.Model.Stock.Product.PriceCheck
{
    public class Request
    {
        public List<Item> Items { get; set; } = new();
    }

    public class Item
    {
        public string? Key { get; set; }
        public decimal Price { get; set; }
        public string? StockNumber { get; set; }
        public string? SkuCode { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? MoldDesign { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public decimal? GoldWeight { get; set; }
        public decimal? GemPrice { get; set; }
    }
}
