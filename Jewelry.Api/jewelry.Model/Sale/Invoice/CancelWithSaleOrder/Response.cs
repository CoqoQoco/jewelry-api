namespace jewelry.Model.Sale.Invoice.CancelWithSaleOrder
{
    public class Response
    {
        public string InvoiceNumber { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public int CancelledPaymentCount { get; set; }
        public string Message { get; set; } = null!;
    }
}
