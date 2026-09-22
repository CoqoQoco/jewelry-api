using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Stock.StockProductGallery.Upload;

public class Request
{
    public string StockNumber { get; set; } = null!;
    public string Scope { get; set; } = null!;
    public IFormFile Image { get; set; } = null!;
}
