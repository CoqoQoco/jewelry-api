using System;
using System.Collections.Generic;

namespace jewelry.Model.Catalog.List
{
    public class Response
    {
        public int Total { get; set; }
        public List<ResponseRow> Items { get; set; } = new List<ResponseRow>();
    }

    public class ResponseRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string NameTh { get; set; } = null!;
        public string? NameEn { get; set; }
        public string? CollectionTitle { get; set; }
        public string? HeaderLabel { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }
}
