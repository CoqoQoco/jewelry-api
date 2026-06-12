using System;

namespace jewelry.Model.Sale.SaleDocumentCatalog
{
    public class ListResponse
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
        public int ItemCount { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
