using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.ProductGroup
{
    public class Response
    {
        public List<GroupData> Groups { get; set; } = new List<GroupData>();
        public decimal TotalPieceQty { get; set; }
        public int TotalPieceRowCount { get; set; }
    }

    public class GroupData
    {
        public string Key { get; set; } = null!;
        public string? Label { get; set; }
        public decimal PieceQty { get; set; }
        public int PieceRowCount { get; set; }
        public decimal Percent { get; set; }
        public int AvgAgeDays { get; set; }
    }
}
