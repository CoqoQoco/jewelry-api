using System;

namespace jewelry.Model.Sale.BillingNote.Update
{
    public class Request
    {
        public string Running { get; set; } = null!;
        public DateTimeOffset DocumentDate { get; set; }
        public int GoldResizeQty { get; set; }
        public decimal GoldResizePerUnit { get; set; }
        public int SilverResizeQty { get; set; }
        public decimal SilverResizePerUnit { get; set; }
        public bool HasSupport { get; set; }
        public decimal SupportPercent { get; set; }
        public decimal VatPercent { get; set; }
        public string? Remark { get; set; }
    }
}
