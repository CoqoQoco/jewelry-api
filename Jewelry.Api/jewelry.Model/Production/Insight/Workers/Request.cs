using System;

namespace jewelry.Model.Production.Insight.Workers
{
    public class Request
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        // null/ว่าง = ทุกแผนก (trim/rawPolish/gemSort/setting/plating)
        public string[]? DepartmentKeys { get; set; }

        // null/ว่าง = ทุกประเภทการจ้าง — ค่า: 'IN_HOUSE' | 'OUTSIDE' | 'SHOP'
        public string[]? EmploymentTypes { get; set; }
    }
}
