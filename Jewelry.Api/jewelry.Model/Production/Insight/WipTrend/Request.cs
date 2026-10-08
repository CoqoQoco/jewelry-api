using System;

namespace jewelry.Model.Production.Insight.WipTrend
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // 'week' | 'month'
        public string Bucket { get; set; } = "week";
    }
}
