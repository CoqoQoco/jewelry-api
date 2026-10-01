using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.GoldStagePendingReturn
{
    public class Request : DataSourceRequest
    {
        public string Metal { get; set; } = "GOLD";
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
        public string[]? DepartmentKeys { get; set; }
        public int OlderThanDays { get; set; } = 14;

        // false (default) = แสดงเฉพาะของที่มีช่างจริงถือครองอยู่ (pendingWithWorker) — true = รวมของที่ยังอยู่ใน
        // คิว (placeholder/ไม่มีช่าง) ด้วย แต่ละแถวจะมี isQueue บอกว่าเป็นของคิวหรือไม่
        public bool IncludeQueue { get; set; } = false;
    }
}
