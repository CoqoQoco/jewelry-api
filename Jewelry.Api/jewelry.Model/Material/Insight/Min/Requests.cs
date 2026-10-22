namespace jewelry.Model.Material.Insight.Min
{
    // upsert ตาม (Kind, ItemKey); gold ItemKey = "{GoldCode}|{GoldSizeCode}"
    public class SaveRequest
    {
        public string Kind { get; set; } = "GOLD";
        public string ItemKey { get; set; } = null!;
        public decimal MinQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Note { get; set; }
    }

    public class DeleteRequest
    {
        public string Kind { get; set; } = "GOLD";
        public string ItemKey { get; set; } = null!;
    }

    public class Response
    {
        public string Kind { get; set; } = "GOLD";
        public string ItemKey { get; set; } = null!;
        public decimal MinQty { get; set; }
        public int? LeadTimeDays { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; }
    }
}
