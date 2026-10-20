namespace jewelry.Model.Stock.Product.PriceReviewAck
{
    public class AckRequest
    {
        public List<string> StockNumbers { get; set; } = new();
        public string? Note { get; set; }
    }

    public class UnackRequest
    {
        public List<string> StockNumbers { get; set; } = new();
    }

    public class Response
    {
        public int Count { get; set; }
    }
}
