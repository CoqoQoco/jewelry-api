using System;

namespace jewelry.Model.Production.Insight.Wip
{
    public class Request
    {
        public int StaleDays { get; set; } = 180;
        public int RiskWindowDays { get; set; } = 30;

        // ช่วงที่ใช้กับ report.flow / FC_BOTTLENECK / WIP_DEPT_GROWING เท่านั้น — กฎอื่น (stale/overdue/
        // melted/becoming-stale/due-soon) ยึด "ตอนนี้" เสมอ ไม่สนช่วงนี้ — ไม่ส่งมา = ย้อนหลัง 90 วันจากตอนนี้ (เดิม)
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public int GrowthThresholdPercent { get; set; } = 20;
    }
}
