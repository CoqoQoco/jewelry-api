using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.Gold
{
    public class Request
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";

        // null/ว่าง = ทั้งสองประเภท (50 ช่างแต่ง, 80 ช่างฝัง)
        public int[]? WorkerTypes { get; set; }
        public string[]? WorkerCodes { get; set; }

        // 'GOLD' (default) | 'SILVER' — ใบ gold loss ปนทอง/เงิน ราคาต่างกันมาก คำนวณแยกทีละโลหะเท่านั้น
        public string Metal { get; set; } = "GOLD";

        // what-if: override เป้าหมาย % ที่บันทึกไว้ เฉพาะ call นี้ (ไม่บันทึกลง DB)
        public List<DraftTargetItem>? DraftTargets { get; set; }
    }

    public class DraftTargetItem
    {
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }
    }
}
