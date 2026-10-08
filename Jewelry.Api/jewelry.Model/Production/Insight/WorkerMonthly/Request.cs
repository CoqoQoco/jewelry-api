using System;

namespace jewelry.Model.Production.Insight.WorkerMonthly
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public string Code { get; set; } = null!;
        public string DeptKey { get; set; } = null!;
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }
    }
}
