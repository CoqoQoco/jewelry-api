using System.Collections.Generic;

namespace jewelry.Model.Stock.Balance.Summary
{
    public class Response
    {
        public OverallItem Overall { get; set; } = new();
        public List<ByLocationItem> ByLocation { get; set; } = new();
    }

    public class OverallItem
    {
        public int SkuCount { get; set; }
        public int LocationCount { get; set; }
        public decimal TotalOnHand { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalAvailable { get; set; }
    }

    public class ByLocationItem
    {
        public string LocationCode { get; set; } = null!;
        public string? LocationName { get; set; }
        public int SkuCount { get; set; }
        public decimal TotalOnHand { get; set; }
        public decimal TotalReserved { get; set; }
        public decimal TotalAvailable { get; set; }
    }
}
