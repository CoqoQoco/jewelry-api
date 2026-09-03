namespace jewelry.Model.Print.Enqueue
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public string Payload { get; set; } = null!;
    }
}
