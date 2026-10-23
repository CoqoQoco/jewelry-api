namespace jewelry.Model.Report.Executive.DataQuality
{
    public class Request
    {
        // 'ALL' (default) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        // override threshold ราย field (null = ใช้ค่า default) — request ที่มี override ข้าม cache
        public ThresholdOverrides? Thresholds { get; set; }
    }

    public class ThresholdOverrides
    {
        public decimal? WarnPercent { get; set; }
        public decimal? CriticalPercent { get; set; }
    }
}
