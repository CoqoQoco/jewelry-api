using System;

namespace jewelry.Model.Production.Insight.GoldStagePendingReturn
{
    public class Item
    {
        public int PlanId { get; set; }
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string WoText { get; set; } = null!;
        public string DeptKey { get; set; } = null!;
        public string? WorkerCode { get; set; }

        // null ถ้าไม่มีรหัสช่างเลย — ถ้ามี อาจเป็นชื่อช่างจริงหรือชื่อ placeholder (เช่น "รอจ่ายขัดชุบ 9K") ก็ได้
        public string? WorkerName { get; set; }

        // true = ยังอยู่ในคิว (placeholder/ไม่มีช่าง ยังไม่ออกให้ใครจริง) — false = มีช่างจริงถือครองอยู่
        public bool IsQueue { get; set; }

        // header.create_date
        public DateTime SentDate { get; set; }

        public decimal SendGram { get; set; }
        public double DaysSince { get; set; }
    }
}
