namespace jewelry.Model.Material.Insight.Policy
{
    public class GetRequest
    {
        public string Kind { get; set; } = "GOLD";
    }

    // field ที่เป็น null = คงค่าเดิม (partial update); ค่าที่ส่งมาถูก clamp ตามช่วงที่อนุญาต
    public class SaveRequest
    {
        public string Kind { get; set; } = "GOLD";
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

    public class Response
    {
        public string Kind { get; set; } = "GOLD";
        public PolicyValues Policy { get; set; } = new PolicyValues();
    }
}
