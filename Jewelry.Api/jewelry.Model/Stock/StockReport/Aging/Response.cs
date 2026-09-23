using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.Aging
{
    public class Response
    {
        public List<BucketData> Buckets { get; set; } = new List<BucketData>();
        public List<TopGroupData> TopGroups { get; set; } = new List<TopGroupData>();
        public decimal AgedQty { get; set; }
        public int AgedRowCount { get; set; }
        public int ThresholdYears { get; set; }
        public decimal TotalPieceQty { get; set; }
    }

    public class BucketData
    {
        public string Key { get; set; } = null!;
        public string? Label { get; set; }
        public decimal PieceQty { get; set; }
        public int PieceRowCount { get; set; }
    }

    public class TopGroupData
    {
        public string Key { get; set; } = null!;
        public string? Label { get; set; }
        public decimal PieceQty { get; set; }
        public decimal AgedQty { get; set; }
        public decimal AgedPercent { get; set; }
    }
}
