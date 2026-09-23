using System.Collections.Generic;

namespace jewelry.Model.Stock.StockReport.ProductGroup
{
    public class Request
    {
        public List<string>? LocationCodes { get; set; }
        public List<string>? ProductTypes { get; set; }
        public List<string>? Golds { get; set; }
        public List<string>? GoldSizes { get; set; }
        public string? GroupBy { get; set; }
    }
}
