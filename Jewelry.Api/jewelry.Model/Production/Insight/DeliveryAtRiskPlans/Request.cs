using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.DeliveryAtRiskPlans
{
    public class Request : DataSourceRequest
    {
        public int RiskHorizonDays { get; set; } = 30;
        public string[]? DepartmentKeys { get; set; }
    }
}
