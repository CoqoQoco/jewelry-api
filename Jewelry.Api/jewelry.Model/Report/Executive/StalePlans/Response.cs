using System;

namespace jewelry.Model.Report.Executive.StalePlans
{
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
    }
}
