using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Availability
{
    public class Request
    {
        public List<string> StockNumbers { get; set; } = new();
    }
}
