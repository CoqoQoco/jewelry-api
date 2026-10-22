namespace jewelry.Model.Sale.Insight.Overview
{
    public class Request
    {
        // 'ALL' (default) | 'GOLD' | 'SILVER' — ค่าอื่นถือเป็น ALL
        public string Metal { get; set; } = "ALL";

        // จำนวนเดือนย้อนหลังของ series (รวมเดือนปัจจุบัน) — clamp 3..36
        public int Months { get; set; } = 12;

        // override threshold ราย field (null = ใช้ค่า default)
        public ThresholdOverrides? Thresholds { get; set; }
    }

    public class ThresholdOverrides
    {
        public decimal? OverdueWarnPercent { get; set; }
        public decimal? OverdueCriticalPercent { get; set; }
        public int? OldestOverdueDays { get; set; }
        public decimal? NoDueDateWarnPercent { get; set; }
        public decimal? ConcentrationPercent { get; set; }
        public int? CollectionWindowDays { get; set; }
        public int? BecomingOverdueDays { get; set; }
        public int? SoDeliveryDays { get; set; }
        public int? MinHistoryMonths { get; set; }
    }
}
