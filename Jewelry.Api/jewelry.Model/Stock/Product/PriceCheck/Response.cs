namespace jewelry.Model.Stock.Product.PriceCheck
{
    public class Response
    {
        public List<ResultItem> Items { get; set; } = new();
    }

    public class ResultItem
    {
        public string? Key { get; set; }
        public bool IsOutlier { get; set; }
        public List<Reason> Reasons { get; set; } = new();
        public decimal? SuggestedPrice { get; set; }
    }

    public class Reason
    {
        public string Code { get; set; } = null!;
        public decimal Value { get; set; }
        public decimal Reference { get; set; }
    }
}
