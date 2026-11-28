using System;

namespace jewelry.Model.Sale.BillingNote.AvailableInvoices
{
    public class Response
    {
        public string InvoiceRunning { get; set; } = null!;
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public bool VatIncluded { get; set; }
        public decimal VatAmount { get; set; }
        public decimal GrandTotalRounded { get; set; }
        public string CustomerName { get; set; } = null!;
    }
}
