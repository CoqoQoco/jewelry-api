using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.AgingItems
{
    public class Request
    {
        public List<string>? LocationCodes { get; set; }
        public List<string>? ProductTypes { get; set; }
        public List<string>? Golds { get; set; }
        public List<string>? GoldSizes { get; set; }
        public int ThresholdYears { get; set; } = 2;
        public int Skip { get; set; }
        public int Take { get; set; } = 50;
    }
}
