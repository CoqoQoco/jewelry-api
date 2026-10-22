namespace jewelry.Model.Material.Insight
{
    // policy ราย field (null = ไม่ override) — ใช้ทั้ง what-if ใน Overview.Request และ Policy/Save
    public class PolicyOverrides
    {
        public int? WindowMonths { get; set; }
        public int? LeadTimeDays { get; set; }
        public int? SafetyDays { get; set; }
        public decimal? FloorQty { get; set; }
        public decimal? NearPercent { get; set; }
        public int? DeadStockDays { get; set; }
        public int? ForecastMonths { get; set; }
        public int? OnProcessStaleDays { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public decimal? ConcentrationPercent { get; set; }
        public int? MinHistoryMonths { get; set; }
    }

    // policy ที่ใช้จริง (หลัง merge request > db > default แล้ว clamp)
    public class PolicyValues
    {
        public int WindowMonths { get; set; }
        public int LeadTimeDays { get; set; }
        public int SafetyDays { get; set; }
        public decimal FloorQty { get; set; }
        public decimal NearPercent { get; set; }
        public int DeadStockDays { get; set; }
        public int ForecastMonths { get; set; }
        public int OnProcessStaleDays { get; set; }
        public decimal PriceChangePercent { get; set; }
        public decimal ConcentrationPercent { get; set; }
        public int MinHistoryMonths { get; set; }

        // 'db' | 'default' | 'request' — request = มี field ใดถูก override จาก request (what-if)
        public string Source { get; set; } = "default";
    }
}
