using System;

namespace jewelry.Model.Production.Insight.Delivery
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";

        public int RiskHorizonDays { get; set; } = 30;

        // what-if: override เป้าหมาย % ที่บันทึกไว้ เฉพาะ call นี้ (ไม่บันทึกลง DB)
        public decimal? DraftTargetPercent { get; set; }
    }
}
