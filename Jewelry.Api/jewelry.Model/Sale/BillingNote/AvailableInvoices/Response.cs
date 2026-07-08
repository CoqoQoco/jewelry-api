using System;

namespace jewelry.Model.Sale.BillingNote.AvailableInvoices
{
    public class Response
    {
        public string InvoiceRunning { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public string CustomerName { get; set; } = null!;
    }
}
