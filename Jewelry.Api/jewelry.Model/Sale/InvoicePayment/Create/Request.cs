using Microsoft.AspNetCore.Http;
using System;

namespace jewelry.Model.Sale.InvoicePayment.Create
{
    public class Request
    {
        public string InvoiceNumber { get; set; } = null!;
        public DateTimeOffset PaymentDate { get; set; }
        public decimal Amount { get; set; }

        public int Payment { get; set; }
        public string PaymentName { get; set; }

        public string? ReferenceNumber { get; set; }
        public string? Remark { get; set; }

        public IFormFile? ReceiptImage { get; set; }
    }
}
