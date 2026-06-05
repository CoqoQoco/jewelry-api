using System;
using System.Collections.Generic;

namespace jewelry.Model.Catalog.Get
{
    public class Response
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string NameTh { get; set; } = null!;
        public string? NameEn { get; set; }
        public string? CollectionTitle { get; set; }
        public string? HeaderLabel { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
        public List<ResponseItem> Items { get; set; } = new List<ResponseItem>();
    }

    public class ResponseItem
    {
        public long Id { get; set; }
        public string ProductNumber { get; set; } = null!;
        public int SortOrder { get; set; }
        public string? Dimension1 { get; set; }
        public string? Dimension2 { get; set; }
        public string? Dimension3 { get; set; }
    }
}
