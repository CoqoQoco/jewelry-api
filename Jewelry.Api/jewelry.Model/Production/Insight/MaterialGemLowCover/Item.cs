namespace jewelry.Model.Production.Insight.MaterialGemLowCover
{
    public class Item
    {
        public string Code { get; set; } = null!;
        public string GroupName { get; set; } = null!;
        public string Shape { get; set; } = null!;
        public string Size { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal Quantity { get; set; }

        // รวมจำนวนเบิก (type 7) 90 วันล่าสุด
        public decimal Used90d { get; set; }

        // Quantity ÷ (Used90d/90)
        public double CoverDays { get; set; }
    }
}
