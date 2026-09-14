using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Certificate.UploadImage
{
    public class Request
    {
        public IFormFile Image { get; set; } = null!;
        public string Kind { get; set; } = null!;
    }
}
