using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Image.BulkPreview
{
    public class Request
    {
        public List<string> StockNumbers { get; set; } = new List<string>();
        public bool IncludeSameMoldInReceipt { get; set; }
    }
}
