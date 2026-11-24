using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Image.SuggestByProduct
{
    public class Request
    {
        public List<Item> Items { get; set; } = new List<Item>();
    }

    public class Item
    {
        public string? Key { get; set; }
        public string? ProductNumber { get; set; }
    }
}
