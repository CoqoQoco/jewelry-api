namespace jewelry.Model.Sale.SaleDocument
{
    public class ListResponse
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string BlobPath { get; set; }
        public int DocumentMonth { get; set; }
        public int DocumentYear { get; set; }
        public string? Tags { get; set; }
        public string? Remark { get; set; }
        public string? CreateBy { get; set; }
        public DateTime? CreateDate { get; set; }
    }
}
