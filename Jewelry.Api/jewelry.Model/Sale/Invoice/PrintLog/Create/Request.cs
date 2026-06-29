namespace jewelry.Model.Sale.Invoice.PrintLog.Create
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public string PaperType { get; set; } = null!;
        public string? Data { get; set; }
    }
}
