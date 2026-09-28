using System.Collections.Generic;

namespace jewelry.Model.Report.Executive.StockHealth
{
    public class Response
    {
        public List<AgeBucketData> AgeBuckets { get; set; } = new List<AgeBucketData>();
        public List<ReceiptTypeData> ReceiptTypes { get; set; } = new List<ReceiptTypeData>();
    }

    public class AgeBucketData
    {
        public string Key { get; set; } = null!;
        public int Count { get; set; }
        public int NoCostCount { get; set; }
        public decimal CostThb { get; set; }
    }

    public class ReceiptTypeData
    {
        public string? ReceiptType { get; set; }
        public int Count { get; set; }
        public int NoCostCount { get; set; }
    }
}
