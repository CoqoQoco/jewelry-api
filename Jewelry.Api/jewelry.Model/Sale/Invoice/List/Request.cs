using Kendo.DynamicLinqCore;

namespace jewelry.Model.Sale.Invoice.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string? InvoiceNumber { get; set; }
        public string? DKInvoiceNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerCode { get; set; }
        public int? Status { get; set; }
        public string? CreateBy { get; set; }

        public DateTimeOffset? CreateDateFrom { get; set; }
        public DateTimeOffset? CreateDateTo { get; set; }

        public DateTimeOffset? DeliveryDateFrom { get; set; }
        public DateTimeOffset? DeliveryDateTo { get; set; }
    }
}
