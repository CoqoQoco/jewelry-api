using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.ProductionWip
{
    public class Response
    {
        public List<DepartmentData> Departments { get; set; } = new List<DepartmentData>();
        public List<MonthlyCompletedData> MonthlyCompleted { get; set; } = new List<MonthlyCompletedData>();
    }

    public class DepartmentData
    {
        public string Key { get; set; } = null!;
        public List<int> StatusIds { get; set; } = new List<int>();
        public int Total { get; set; }
        public int Moved30d { get; set; }
        public int Moved30to180d { get; set; }
        public int Stale180d { get; set; }
    }

    public class MonthlyCompletedData
    {
        public string Month { get; set; } = null!;
        public int CompletedCount { get; set; }
    }
}
