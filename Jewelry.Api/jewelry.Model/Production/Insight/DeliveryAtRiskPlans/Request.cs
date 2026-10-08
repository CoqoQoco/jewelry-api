using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.DeliveryAtRiskPlans
{
    public class Request : DataSourceRequest
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public int RiskHorizonDays { get; set; } = 30;
        public string[]? DepartmentKeys { get; set; }
    }
}
