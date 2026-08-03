using System.Collections.Generic;

namespace jewelry.Model.Stock.Gem.Dashboard
{
    public class AgingReportResponse
    {
        public List<AgingBucket> Buckets { get; set; } = new List<AgingBucket>();
        public int TotalGemCodes { get; set; }
        public decimal TotalValue { get; set; }
        public int DeadStockCodes { get; set; }
        public decimal DeadStockValue { get; set; }
    }

    public class AgingBucket
    {
        public string BucketKey { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public int GemCodes { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalQuantityWeight { get; set; }
        public decimal TotalValue { get; set; }
    }
}
