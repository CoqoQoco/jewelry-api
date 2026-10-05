namespace jewelry.Model.Production.Insight.MaterialGemDemand
{
    // spec = (gem, shape, size, metal) รวมทุกบรรทัดวัตถุดิบพลอยของแผนที่ยังไม่ผ่านแผนกตัดพลอย (ไม่ stale) เข้าด้วยกัน
    public class Item
    {
        public string? Gem { get; set; }
        public string? Shape { get; set; }
        public string? Size { get; set; }
        public string Metal { get; set; } = null!;

        // จำนวนแผน (ไม่ซ้ำ) ที่ต้องการ spec นี้ และอยู่ที่แผนกตัดพลอยแล้ว (สถานะ 69/70)
        public int WaitingPlans { get; set; }

        // จำนวนแผน (ไม่ซ้ำ) ที่ต้องการ spec นี้ แต่ยังไม่ถึงแผนกตัดพลอย (สถานะ 10,49,50,59,60)
        public int UpcomingPlans { get; set; }

        // รวมจำนวนที่ต้องใช้ทุกแผน (ทุกบรรทัดของ spec นี้)
        public decimal RequiredQty { get; set; }

        // null ถ้า unmatched
        public decimal? Available { get; set; }

        // null ถ้าไม่มีการเบิก (type 7) ของรหัส stock ที่ตรง spec นี้ใน 90 วันล่าสุด
        public decimal? UsedPerMonth { get; set; }

        // Available ÷ (usedPerMonth/30) — null ถ้า usedPerMonth เป็น null/0 หรือ Available เป็น null
        public double? CoverDays { get; set; }

        // 'ready' | 'short' | 'unmatched' (เทียบ Available กับ RequiredQty รวม)
        public string Status { get; set; } = null!;

        // เฉพาะ unmatched: 'gem' | 'spec' — null ถ้า ready/short
        public string? UnmatchedReason { get; set; }
    }
}
