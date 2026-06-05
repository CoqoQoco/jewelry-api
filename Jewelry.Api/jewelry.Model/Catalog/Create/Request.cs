using System.Collections.Generic;

namespace jewelry.Model.Catalog.Create
{
    public class Request
    {
        public string Code { get; set; } = null!;
        public string NameTh { get; set; } = null!;
        public string? NameEn { get; set; }
        public string? CollectionTitle { get; set; }
        public string? HeaderLabel { get; set; }
        public List<RequestItem>? Items { get; set; }
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
