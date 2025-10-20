namespace jewelry.Model.Sale.Invoice.List
{
    public class Request
    {
        public string? InvoiceNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerCode { get; set; }
        public string? SoNumber { get; set; }
        public int? Status { get; set; }
        public DateTime? CreateDateFrom { get; set; }
        public DateTime? CreateDateTo { get; set; }
        public DateTime? DeliveryDateFrom { get; set; }
        public DateTime? DeliveryDateTo { get; set; }
        public string? CreateBy { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 10;
        public string? OrderBy { get; set; }
        public string? OrderDirection { get; set; } = "DESC";
    }
}