using Kendo.DynamicLinqCore;

namespace jewelry.Model.Production.Insight.MaterialGemLowCover
{
    public class Request : DataSourceRequest
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public int CoverDays { get; set; } = 30;
    }
}
