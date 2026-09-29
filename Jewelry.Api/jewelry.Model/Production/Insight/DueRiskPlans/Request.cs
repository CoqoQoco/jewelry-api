using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.DueRiskPlans
{
    public class Request : DataSourceRequest
    {
        // 'overdue' | 'dueSoon'
        public string Mode { get; set; } = "overdue";

        // ใช้เฉพาะ mode='dueSoon' — กำหนดวัน (default เท่ากับ ProductionInsight/Wip riskWindowDays)
        public int RiskWindowDays { get; set; } = 30;
    }
}
