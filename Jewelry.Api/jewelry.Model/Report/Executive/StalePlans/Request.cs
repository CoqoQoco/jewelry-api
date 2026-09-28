using Kendo.DynamicLinqCore;

namespace jewelry.Model.Report.Executive.StalePlans
{
    public class Request : DataSourceRequest
    {
        public int MinDays { get; set; } = 180;
        public string[]? DepartmentKeys { get; set; }
    }
}
