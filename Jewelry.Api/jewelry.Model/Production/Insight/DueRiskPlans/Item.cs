using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Insight.DueRiskPlans
{
    public class WorkerItem
    {
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsQueue { get; set; }
    }

    public class Item
    {
        public int PlanId { get; set; }
        public string Wo { get; set; } = null!;
        public int WoNumber { get; set; }
        public string WoText { get; set; } = null!;
        public string Mold { get; set; } = null!;
        public string ProductNumber { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public int ProductQty { get; set; }
        public int StatusId { get; set; }
        public string? StatusName { get; set; }
        public string? DepartmentKey { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastMoveDate { get; set; }
        public int DaysSinceMove { get; set; }
        public DateTime DueDate { get; set; }

        // ติดลบ = เกินกำหนดแล้ว, บวก = ยังไม่ถึงกำหนด
        public int DaysToDue { get; set; }

        // เสริมหลัง paging เท่านั้น — มาจาก header ล่าสุดของแผน (ดู ProductionPlanWipHelper.GetLastActionInfoAsync)
        public string? LastUpdateBy { get; set; }
        public string? LastAction { get; set; }
        public string? LastActionRemark { get; set; }
        public DateTime LastActionDate { get; set; }
        public List<string> Workers { get; set; } = new List<string>();
        public List<WorkerItem> WorkerItems { get; set; } = new List<WorkerItem>();
    }
}
