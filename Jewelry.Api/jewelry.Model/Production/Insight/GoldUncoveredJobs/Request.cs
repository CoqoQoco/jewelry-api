using System;
using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.GoldUncoveredJobs
{
    public class Request : DataSourceRequest
    {
        public int[]? WorkerTypes { get; set; }
        public string[]? WorkerCodes { get; set; }

        // 'GOLD' (default) | 'SILVER'
        public string Metal { get; set; } = "GOLD";

        // บังคับส่ง — ต้องเป็นช่วงเดียวกับที่ใช้คำนวณ coverage ของ KPI (Gold) เพื่อให้ตัวเลข coverage ตรงกับตาราง
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // ตัวกรองเสริมซ้อนด้านบน (ไม่ใช่ทางเลือกแทน start/end อีกต่อไป) — 0 = ไม่กรองเพิ่ม
        public int OlderThanDays { get; set; }
    }
}
