using System;

namespace jewelry.Model.Production.Insight.Delivery
{
    public class Request
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";

        public int RiskHorizonDays { get; set; } = 30;

        // what-if: override เป้าหมาย % ที่บันทึกไว้ เฉพาะ call นี้ (ไม่บันทึกลง DB)
        public decimal? DraftTargetPercent { get; set; }
    }
}
