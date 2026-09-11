namespace jewelry.Model.Sale.Invoice.Delete
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public string DeleteReason { get; set; } = null!;
    }
}