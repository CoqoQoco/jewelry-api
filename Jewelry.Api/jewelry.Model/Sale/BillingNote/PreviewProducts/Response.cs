namespace jewelry.Model.Sale.BillingNote.PreviewProducts
{
    public class Response
    {
        public string InvoiceRunning { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
    }
}
