using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.GoldByStage
{
    public class Request
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'GOLD' (default) | 'SILVER'
        public string Metal { get; set; } = "GOLD";

        // what-if: override เป้าหมาย % ที่บันทึกไว้ เฉพาะ call นี้ (ไม่บันทึกลง DB) — ใช้เฉพาะรายการ scope=STAGE
        // ที่ metal ตรงกับ request.Metal เท่านั้น (ส่ง scope=SLIP มาด้วยได้ แต่ไม่มีผลใน endpoint นี้)
        public List<DraftTargetItem>? DraftTargets { get; set; }
    }

    public class DraftTargetItem
    {
        // 'SLIP' | 'STAGE' (endpoint นี้ใช้เฉพาะ STAGE)
        public string Scope { get; set; } = "SLIP";

        // scope=STAGE: worker_type คือสถานะทำงานของแผนก (60 rawPolish, 80 setting, 90 plating)
        public int WorkerType { get; set; }
        public string Metal { get; set; } = "GOLD";
        public decimal TargetPercent { get; set; }
    }
}
