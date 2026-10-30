namespace jewelry.Model.Material.Insight.Overview
{
    public class Request
    {
        // 'GOLD' (default) | 'GEM'
        public string Kind { get; set; } = "GOLD";

        // จำนวนเดือนย้อนหลังของ series (รวมเดือนปัจจุบัน) — clamp 3..36
        public int Months { get; set; } = 12;

        // what-if: override policy ราย field โดยไม่บันทึกลงตาราง (null = ใช้ค่าจาก db/default)
        public PolicyOverrides? Thresholds { get; set; }
    }
}
