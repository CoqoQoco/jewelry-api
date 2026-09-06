using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.UpdatePriceBulk
{
    public class Request
    {
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        public string SkuCode { get; set; }
        public decimal Price { get; set; }
    }
}
