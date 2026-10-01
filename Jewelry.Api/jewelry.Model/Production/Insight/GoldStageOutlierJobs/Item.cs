using System;

namespace jewelry.Model.Production.Insight.GoldStageOutlierJobs
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

        // detail.request_date ?? header.create_date
        public DateTime Date { get; set; }

        public decimal SendGram { get; set; }
        public decimal CheckGram { get; set; }
        public decimal DiffGram { get; set; }
        public decimal DiffPercent { get; set; }
        public decimal DeptMedianPercent { get; set; }
    }
}
