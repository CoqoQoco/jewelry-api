using System.Collections.Generic;

namespace jewelry.Model.Catalog.AddProducts
{
    public class Request
    {
        public int CatalogId { get; set; }
        public List<RequestItem> Items { get; set; } = new List<RequestItem>();
    }

    public class RequestItem
    {
        public string ProductNumber { get; set; } = null!;
        public string? Dimension1 { get; set; }
        public string? Dimension2 { get; set; }
        public string? Dimension3 { get; set; }
    }
}
