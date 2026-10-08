using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.DueRiskPlans
{
    public class Request : DataSourceRequest
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        // 'overdue' | 'dueSoon'
        public string Mode { get; set; } = "overdue";

        // ใช้เฉพาะ mode='dueSoon' — กำหนดวัน (default เท่ากับ ProductionInsight/Wip riskWindowDays)
        public int RiskWindowDays { get; set; } = 30;
    }
}
