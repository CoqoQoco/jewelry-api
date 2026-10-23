using System;
using System.Collections.Generic;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;
using ActionItem = jewelry.Model.Production.Insight.Wip.ActionItem;

namespace jewelry.Model.Report.Executive.DataQuality
{
    public class Response
    {
        public DateTime AsOf { get; set; }
        public string Metal { get; set; } = "ALL";

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดของ check ที่ available
        public string Status { get; set; } = "ok";

        public ThresholdValues Thresholds { get; set; } = new ThresholdValues();
        public Kpi Kpi { get; set; } = new Kpi();
        public List<Check> Checks { get; set; } = new List<Check>();
        public List<Finding> Problems { get; set; } = new List<Finding>();
        public List<Finding> Forecasts { get; set; } = new List<Finding>();
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
    }

    public class ThresholdValues
    {
        public decimal WarnPercent { get; set; }
        public decimal CriticalPercent { get; set; }
    }

    // นับเฉพาะ check ที่ available
    public class Kpi
    {
        public int Total { get; set; }
        public int WarningCount { get; set; }
        public int CriticalCount { get; set; }
        public int PassedCount { get; set; }

        // tab ที่มี check ไม่ผ่านอย่างน้อย 1 รายการ (distinct, ตามลำดับ check)
        public List<string> AffectedTabs { get; set; } = new List<string>();
    }

    public class Check
    {
        public string Code { get; set; } = null!;
        public int Count { get; set; }
        public int Total { get; set; }

        // เปอร์เซ็นต์ 0..100 (ปัด 2 ตำแหน่ง) — severity ตัดจากค่าที่ยังไม่ปัด
        public decimal Ratio { get; set; }

        // 'critical' | 'warning' | 'ok'
        public string Severity { get; set; } = "ok";

        // ตัวเลขในแท็บอื่นที่ผิดเพี้ยนเพราะช่องว่างนี้ (stable key ให้ UI map i18n)
        public string Impact { get; set; } = null!;

        // 'stock' | 'material' | 'sales' | 'production'
        public string Tab { get; set; } = null!;
        public string ReportRef { get; set; } = null!;

        // false = โหลดไม่ได้/ยังไม่รองรับ -> ไม่นับใน kpi และไม่ emit problem
        public bool Available { get; set; } = true;

        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    }
}
