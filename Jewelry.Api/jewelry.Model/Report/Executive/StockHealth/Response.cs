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

        // ถ่วงด้วย qty ของ piece (ล็อตเงิน) — Qty = Σ qty, CostThbQty = Σ ProductCost*qty
        public decimal Qty { get; set; }
        public decimal CostThbQty { get; set; }
    }

    public class ReceiptTypeData
    {
        public string? ReceiptType { get; set; }
        public int Count { get; set; }
        public int NoCostCount { get; set; }
        public decimal Qty { get; set; }
    }
}
