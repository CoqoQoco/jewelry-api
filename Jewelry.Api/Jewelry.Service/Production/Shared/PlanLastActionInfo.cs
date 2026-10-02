using System;
using System.Collections.Generic;

namespace Jewelry.Service.Production.Shared
{
    // รหัสช่างที่ resolve ชื่อแล้ว 1 คน — isQueue=true สำหรับ placeholder ("รอจ่าย...") — ช่าง TEST ถูกตัดทิ้งไป
    // ตั้งแต่ตอนสร้าง list นี้แล้ว ไม่มีโอกาสโผล่มาถึงที่นี่
    public class PlanWorkerItem
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsQueue { get; set; }
    }

    // ข้อมูล "ทำอะไรล่าสุด" ของแผนผลิต 1 แผน — มาจาก header ล่าสุด (IsActive) หรือ fallback ไปข้อมูลของแผนเอง
    // ถ้าแผนยังไม่มี header เลย (เช่นสถานะออกแบบ 10)
    public class PlanLastActionInfo
    {
        public string? LastUpdateBy { get; set; }
        public string? LastAction { get; set; }
        public string? LastActionRemark { get; set; }
        public DateTime LastActionDate { get; set; }

        // ชื่อช่างที่ไม่ใช่คิว (isQueue=false) เท่านั้น — เก็บไว้เพื่อ backward compatibility (field เดิม)
        public List<string> Workers { get; set; } = new List<string>();

        // ชุดเต็ม (code+name+isQueue) — ช่างจริงเรียงก่อน คิวเรียงหลัง, ไม่มีช่าง TEST ปน
        public List<PlanWorkerItem> WorkerItems { get; set; } = new List<PlanWorkerItem>();
    }
}
