using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.Wip
{
    public class Response
    {
        public DateTime AsOf { get; set; }

        // 'critical' | 'warning' | 'ok' — severity ที่แย่ที่สุดใน problems+forecasts
        public string Status { get; set; } = "ok";

        public List<Finding> Problems { get; set; } = new List<Finding>();
        public List<Finding> Forecasts { get; set; } = new List<Finding>();
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
        public ReportData Report { get; set; } = new ReportData();
    }

    public class Finding
    {
        public string Code { get; set; } = null!;

        // 'critical' | 'warning' | 'info'
        public string Severity { get; set; } = null!;

        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        // 'departments' | 'flow' | 'stalePlans' | 'dueRisk' | 'trend'
        public string ReportRef { get; set; } = null!;
    }

    public class ActionItem
    {
        public string Code { get; set; } = null!;
        public int Priority { get; set; }
        public string OwnerRole { get; set; } = null!;
        public List<string> RelatedCodes { get; set; } = new List<string>();
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();
    }

    public class ReportData
    {
        public List<DepartmentReportItem> Departments { get; set; } = new List<DepartmentReportItem>();
        public List<FlowReportItem> Flow { get; set; } = new List<FlowReportItem>();
        public int OpenCount { get; set; }
        public int OverdueCount { get; set; }
        public int DueSoonAtRiskCount { get; set; }
        public int BecomingStaleCount { get; set; }
        public int MeltedOpenCount { get; set; }
    }

    public class DepartmentReportItem
    {
        public string Key { get; set; } = null!;
        public int Total { get; set; }
        public int Moved30d { get; set; }
        public int Moved30to180d { get; set; }
        public int Stale180d { get; set; }
    }

    public class FlowReportItem
    {
        public string Key { get; set; } = null!;

        // ชื่อเดิม (compat) — ค่าเดียวกับ Inflow/Outflow เสมอ ไม่ใช่ fix 90 วันอีกต่อไป (ตาม range ของ request)
        public int Inflow90d { get; set; }
        public int Outflow90d { get; set; }

        // ฟิลด์ใหม่ทั่วไป — UI จะสลับมาใช้ตัวนี้แทน
        public int Inflow { get; set; }
        public int Outflow { get; set; }
        public int Net { get; set; }
    }
}
