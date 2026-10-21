namespace jewelry.Model.Stock.Insight.Overview
{
    public class Request
    {
        // 'ALL' (default) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        // จำนวนเดือนย้อนหลังของ series (รวมเดือนปัจจุบัน) — clamp 3..36
        public int Months { get; set; } = 12;

        // override threshold ราย field (null = ใช้ค่า default) — ใช้ what-if จากหน้าจอโดยไม่ต้องมีตาราง
        public ThresholdOverrides? Thresholds { get; set; }
    }

    public class ThresholdOverrides
    {
        public decimal? NoCostWarnPercent { get; set; }
        public decimal? NoCostCriticalPercent { get; set; }
        public decimal? Aged5yWarnPercent { get; set; }
        public decimal? Aged5yCriticalPercent { get; set; }
        public int? NoOutflowMonths { get; set; }
        public decimal? CoverMonthsLow { get; set; }
        public decimal? CoverMonthsHigh { get; set; }
        public int? CoverWindowMonths { get; set; }
        public int? AgingProjectionMonths { get; set; }
        public decimal? AgingRisePointsWarn { get; set; }
        public int? MinHistoryMonths { get; set; }
    }
}
