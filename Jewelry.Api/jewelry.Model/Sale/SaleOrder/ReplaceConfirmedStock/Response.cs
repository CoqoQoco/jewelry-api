namespace jewelry.Model.Sale.SaleOrder.ReplaceConfirmedStock
{
    public class Response
    {
        public long SaleOrderProductId { get; set; }
        public string StockNumber { get; set; } = string.Empty;
        public string? StockNumberOrigin { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
