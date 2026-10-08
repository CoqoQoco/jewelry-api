using System;

namespace jewelry.Model.Production.Insight.Capacity
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // ยึด month เสมอตาม spec ("Monthly buckets by Thai date") — เก็บ field นี้ไว้เพื่อให้ contract เหมือน
        // endpoint อื่นในโมดูล แต่ไม่ได้ใช้เลือก week ได้จริง (ไม่มี option นั้น)
        public string Bucket { get; set; } = "month";

        // 'plan' (default) | 'piece' — ตอนนี้มีผลแค่ว่า kpi.inflow* ตัวไหนถือเป็นค่าเด่น (ทั้งคู่คำนวณ/ส่งออกเสมอ)
        // ตัวชี้วัดอื่น (activeWip, backlogMonths, queueDays, กฎทั้งหมด) อิงจำนวน "แผน" เสมอไม่ว่าจะเลือก unit ไหน
        public string Unit { get; set; } = "plan";

        // ตัวกรองระดับแผน (tbt_production_plan.product_type / type / type_size) — null/ว่าง = ไม่กรอง — มีผลกับ
        // ทุกตัวเลขของ endpoint นี้ (kpi, series, departments, costCardToDone, breakdown, กฎทั้งหมด) เทียบแบบไม่สนตัวพิมพ์
        // ใช้ค่า "" เพื่อเลือกแผนที่ไม่ระบุค่านั้น
        public string[]? ProductTypes { get; set; }
        public string[]? Golds { get; set; }
        public string[]? GoldSizes { get; set; }

        // 'productType' (default) | 'gold' | 'goldSize' — มิติที่ใช้แตก breakdown
        public string? BreakdownBy { get; set; }
    }
}
