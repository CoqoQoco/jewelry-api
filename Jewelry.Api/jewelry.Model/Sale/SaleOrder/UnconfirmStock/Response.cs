namespace jewelry.Model.Sale.SaleOrder.UnconfirmStock
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int UnconfirmedItemsCount { get; set; }
        public List<string> UnconfirmedStockNumbers { get; set; } = new List<string>();
        public DateTime UnconfirmedDate { get; set; }
    }
}
