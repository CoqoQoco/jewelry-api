using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Sale.SaleDocument
{
    public class UploadRequest
    {
        public IFormFile File { get; set; }
        public int DocumentMonth { get; set; }
        public int DocumentYear { get; set; }
        public string? Tags { get; set; }
        public string? Remark { get; set; }
    }
}
