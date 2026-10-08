using Kendo.DynamicLinqCore;

namespace jewelry.Model.Report.Executive.StalePlans
{
    public class Request : DataSourceRequest
    {
        // ตัวกรองโลหะทั้งหน้า: 'ALL' (default = พฤติกรรมเดิม) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        public int MinDays { get; set; } = 180;
        public string[]? DepartmentKeys { get; set; }
    }
}
