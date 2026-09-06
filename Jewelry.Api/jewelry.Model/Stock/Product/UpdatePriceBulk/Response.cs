using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.UpdatePriceBulk
{
    public class Response
    {
        public int Updated { get; set; }
        public int Unchanged { get; set; }
        public List<string> NotFound { get; set; } = new List<string>();
    }
}
