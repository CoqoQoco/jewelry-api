using System.Collections.Generic;

namespace jewelry.Model.Stock.OldStock._9K
{
    public class TopUpSilverRequest
    {
        public string BatchRef { get; set; } = null!;
        public bool DryRun { get; set; }
        public List<TopUpSilverRequestItem> Items { get; set; } = new();
    }

    public class TopUpSilverRequestItem
    {
        public string NoProduct { get; set; } = null!;
        public int Qty { get; set; }
    }

    public class TopUpSilverResponse
    {
        public bool DryRun { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalQtyAdded { get; set; }
        public List<TopUpSilverResultItem> Items { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class TopUpSilverResultItem
    {
        public string NoProduct { get; set; } = null!;
        public string StockNumber { get; set; } = null!;
        public string Location { get; set; } = null!;
        public decimal QtyBefore { get; set; }
        public decimal QtyAfter { get; set; }
        public string? StatusBefore { get; set; }
        public string? StatusAfter { get; set; }
    }
}
