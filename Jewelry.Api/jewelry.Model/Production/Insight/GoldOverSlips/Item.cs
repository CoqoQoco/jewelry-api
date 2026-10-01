using System;

namespace jewelry.Model.Production.Insight.GoldOverSlips
{
    // tang (50): 1 แถว = 1 slip. setting (80): 1 แถว = 1 item ที่เกิน allowance (ใช้ documentNo/worker ของ slip แม่)
    public class Item
    {
        public long SlipId { get; set; }
        public string DocumentNo { get; set; } = null!;
        public int WorkerType { get; set; }
        public string WorkerCode { get; set; } = null!;
        public string? WorkerName { get; set; }
        public DateTime? RequestDateStart { get; set; }
        public DateTime? RequestDateEnd { get; set; }
        public decimal RawLossGram { get; set; }
        public decimal AllowedGram { get; set; }
        public decimal ExcessGram { get; set; }
        public decimal ExcessMoney { get; set; }
        public decimal NetMoney { get; set; }
    }
}
