using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.StageLeadTime
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";

        // what-if: override ค่ามาตรฐานที่บันทึกไว้ เฉพาะ call นี้ (ไม่บันทึกลง DB)
        public List<DraftStandardItem>? DraftStandards { get; set; }
    }

    public class DraftStandardItem
    {
        public string DeptKey { get; set; } = null!;
        public decimal StandardDays { get; set; }
    }
}
