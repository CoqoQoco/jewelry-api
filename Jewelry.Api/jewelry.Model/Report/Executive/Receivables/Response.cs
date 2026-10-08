using System;

namespace jewelry.Model.Report.Executive.Receivables
{
    public class Item
    {
        public string Running { get; set; } = null!;
        public string? DkInvoiceNumber { get; set; }
        public string SoRunning { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string CurrencyUnit { get; set; } = null!;
        public decimal GrandTotal { get; set; }
        public decimal GrandTotalThb { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Outstanding { get; set; }
        public string PaymentState { get; set; } = null!;
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public string? SalePerson { get; set; }
        public string? SaleChannelCode { get; set; }

        // สัดส่วนของบิลที่เป็นโลหะที่ขอ (0-1) — ALL = 1 เสมอ; GOLD/SILVER = สัดส่วนยอดบรรทัด (NetPrice*Qty) ของโลหะนั้นต่อทั้งบิล
        // ยอดเงินทุกช่อง (grandTotal, grandTotalThb, paidAmount, outstanding) ถูกคูณด้วยค่านี้แล้ว
        public decimal MetalShare { get; set; } = 1m;
    }
}
