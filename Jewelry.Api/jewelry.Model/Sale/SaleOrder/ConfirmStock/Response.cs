namespace jewelry.Model.Sale.SaleOrder.ConfirmStock
{
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ConfirmedItemsCount { get; set; }
        public List<string> ConfirmedStockNumbers { get; set; } = new List<string>();
        public DateTime ConfirmedDate { get; set; }
    }
}