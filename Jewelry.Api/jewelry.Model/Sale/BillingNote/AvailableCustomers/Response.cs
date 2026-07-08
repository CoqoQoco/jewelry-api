namespace jewelry.Model.Sale.BillingNote.AvailableCustomers
{
    public class Response
    {
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string? CustomerAddress { get; set; }
        public string? CustomerTel { get; set; }
        public int InvoiceCount { get; set; }
    }
}
