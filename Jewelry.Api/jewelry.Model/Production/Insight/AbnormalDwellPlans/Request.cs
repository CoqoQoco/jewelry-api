using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.AbnormalDwellPlans
{
    public class Request : DataSourceRequest
    {
        public string[]? DepartmentKeys { get; set; }
        public decimal Multiplier { get; set; } = 2;
    }
}
