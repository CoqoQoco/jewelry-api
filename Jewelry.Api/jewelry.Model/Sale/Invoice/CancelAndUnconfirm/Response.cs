namespace jewelry.Model.Sale.Invoice.CancelAndUnconfirm
{
    public class Response
    {
        public string InvoiceNumber { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public int CancelledPaymentCount { get; set; }
        public int UnconfirmedItemCount { get; set; }
        public List<string> UnconfirmedStockNumbers { get; set; } = new List<string>();
        public string Message { get; set; } = null!;
    }
}
