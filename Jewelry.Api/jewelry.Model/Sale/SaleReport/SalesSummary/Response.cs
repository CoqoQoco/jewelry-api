namespace jewelry.Model.Sale.SaleReport.SalesSummary
{
    public class Response
    {
        public int InvoiceCount { get; set; }
        public int UncomputableInvoiceCount { get; set; }
        public decimal TotalAmountThb { get; set; }
        public decimal PaidAmountThb { get; set; }
        public decimal OutstandingAmountThb { get; set; }
        public decimal OverpaidAmountThb { get; set; }
        public int PaidInvoiceCount { get; set; }
        public int OutstandingInvoiceCount { get; set; }
        public int OverpaidInvoiceCount { get; set; }
        public decimal PieceCount { get; set; }
    }
}
