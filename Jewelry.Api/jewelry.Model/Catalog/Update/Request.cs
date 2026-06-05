using System.Collections.Generic;

namespace jewelry.Model.Catalog.Update
{
    public class Request
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string NameTh { get; set; } = null!;
        public string? NameEn { get; set; }
        public string? CollectionTitle { get; set; }
        public string? HeaderLabel { get; set; }
        public List<RequestItem> Items { get; set; } = new List<RequestItem>();
    }

    public class RequestItem
    {
        public string ProductNumber { get; set; } = null!;
        public int SortOrder { get; set; }
        public string? Dimension1 { get; set; }
        public string? Dimension2 { get; set; }
        public string? Dimension3 { get; set; }
    }
}
