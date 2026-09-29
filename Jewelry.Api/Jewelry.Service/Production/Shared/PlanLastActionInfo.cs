using System;
using System.Collections.Generic;

namespace Jewelry.Service.Production.Shared
{
    // ข้อมูล "ทำอะไรล่าสุด" ของแผนผลิต 1 แผน — มาจาก header ล่าสุด (IsActive) หรือ fallback ไปข้อมูลของแผนเอง
    // ถ้าแผนยังไม่มี header เลย (เช่นสถานะออกแบบ 10)
    public class PlanLastActionInfo
    {
        public string? LastUpdateBy { get; set; }
        public string? LastAction { get; set; }
        public string? LastActionRemark { get; set; }
        public DateTime LastActionDate { get; set; }
        public List<string> Workers { get; set; } = new List<string>();
    }
}
