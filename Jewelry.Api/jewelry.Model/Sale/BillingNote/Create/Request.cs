using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.BillingNote.Create
{
    public class Request
    {
        public string CustomerCode { get; set; } = null!;
        public DateTimeOffset DocumentDate { get; set; }
        public List<string> InvoiceRunnings { get; set; } = new List<string>();

        public int GoldResizeQty { get; set; }
        public decimal GoldResizeAmount { get; set; }
        public int SilverResizeQty { get; set; }
        public decimal SilverResizeAmount { get; set; }

        public decimal VatPercent { get; set; }

        public List<ProductRow> Products { get; set; } = new List<ProductRow>();

        public string? Remark { get; set; }
    }

    public class ProductRow
    {
        public string InvoiceRunning { get; set; } = null!;
        public string? ProductNumber { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public string? Remark { get; set; }
    }
}
