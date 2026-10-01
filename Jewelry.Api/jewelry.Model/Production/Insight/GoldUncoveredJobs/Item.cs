using System;

namespace jewelry.Model.Production.Insight.GoldUncoveredJobs
{
    public class Item
    {
        public int PlanId { get; set; }
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string WoText { get; set; } = null!;

        // 'trim' (ช่างแต่ง, 50) | 'setting' (ช่างฝัง, 80)
        public string? DeptKey { get; set; }
        public string? WorkerCode { get; set; }

        // best-effort จากชื่อช่างหลักของ status header เดียวกัน — ไม่ได้ resolve จาก master worker โดยตรง
        // (status_detail เก็บแต่โค้ด ไม่มีชื่อ) อาจไม่ตรงเป๊ะถ้า Worker/WorkerSub ของงานนี้ไม่ใช่คนเดียวกับช่างหลักของ header
        public string? WorkerName { get; set; }

        public DateTime JobDate { get; set; }
        public decimal SendGram { get; set; }
        public decimal CheckGram { get; set; }
        public decimal DiffGram { get; set; }
        public int DaysSince { get; set; }
    }
}
