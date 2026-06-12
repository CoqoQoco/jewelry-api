using System;
using System.Collections.Generic;

namespace jewelry.Model.Sale.SaleDocumentCatalog
{
    public class GetResponse
    {
        public long Id { get; set; }
        public string? HeaderLabel { get; set; }
        public string? CollectionTitle { get; set; }
        public int? DocumentMonth { get; set; }
        public int? DocumentYear { get; set; }
        public string? Tags { get; set; }
        public string? Remark { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? UpdateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public List<GetItemResponse> Items { get; set; } = new List<GetItemResponse>();
    }

    public class GetItemResponse
    {
        public long Id { get; set; }
        public string? ProductNumber { get; set; }
        public string? DescriptionLine1 { get; set; }
        public string? DescriptionLine2 { get; set; }
        public string? Dimension1 { get; set; }
        public string? Dimension2 { get; set; }
        public string? Dimension3 { get; set; }
        public int SortOrder { get; set; }
        public List<GetItemImageResponse> Images { get; set; } = new List<GetItemImageResponse>();
    }

    public class GetItemImageResponse
    {
        public long Id { get; set; }
        public string BlobPath { get; set; } = null!;
        public int SortOrder { get; set; }
    }
}
