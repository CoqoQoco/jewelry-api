using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.MaterialWaitingPlans
{
    public class Request : DataSourceRequest
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        // null/ว่าง = ทุก gemStatus — ค่า: 'ready' | 'short' | 'unmatched'
        public string? GemStatus { get; set; }
    }
}
