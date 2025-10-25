namespace jewelry.Model.Sale.InvoiceVersion.Upsert
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public string Data { get; set; } = null!;
    }
}
