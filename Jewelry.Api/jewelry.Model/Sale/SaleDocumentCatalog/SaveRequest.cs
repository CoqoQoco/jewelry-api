using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleDocumentCatalog
{
    public class SaveRequest
    {
        public long? Id { get; set; }
        public string? HeaderLabel { get; set; }
        public string? CollectionTitle { get; set; }
        public int? DocumentMonth { get; set; }
        public int? DocumentYear { get; set; }
        public string? Tags { get; set; }
        public string? Remark { get; set; }
        public int Status { get; set; }
        public List<ItemRequest> Items { get; set; } = new List<ItemRequest>();
    }

    public class ItemRequest
    {
        public string? ProductNumber { get; set; }
        public string? DescriptionLine1 { get; set; }
        public string? DescriptionLine2 { get; set; }
        public string? Dimension1 { get; set; }
        public string? Dimension2 { get; set; }
        public string? Dimension3 { get; set; }
        public int SortOrder { get; set; }
        public List<string> ImageBlobPaths { get; set; } = new List<string>();
    }
}
