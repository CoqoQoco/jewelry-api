using System;

namespace jewelry.Model.Report.Executive.SalesOrdersWithoutInvoice
{
    public class Item
    {
        public string Running { get; set; } = null!;
        public string SoNumber { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateTime? SoDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string CurrencyUnit { get; set; } = null!;
        public decimal GrandTotal { get; set; }
        public decimal GrandTotalThb { get; set; }
        public bool IsOverdue { get; set; }
        public string? SalePerson { get; set; }
        public string? SaleChannelCode { get; set; }

        // สัดส่วนของบิลที่เป็นโลหะที่ขอ (0-1) — ALL = 1 เสมอ; GOLD/SILVER = สัดส่วนยอดบรรทัด (NetPrice*Qty) ของโลหะนั้นต่อทั้งบิล
        // ยอดเงินทุกช่อง (grandTotal, grandTotalThb, paidAmount, outstanding) ถูกคูณด้วยค่านี้แล้ว
        public decimal MetalShare { get; set; } = 1m;
    }
}
