namespace jewelry.Model.Certificate.List
{
    public class Request
    {
        public string? InvoiceNumber { get; set; }
        public string? CertificateNo { get; set; }
        public string? StockNumber { get; set; }
        public string? CustomerCode { get; set; }
        public int? Take { get; set; }
        public int? Skip { get; set; }
    }
}
