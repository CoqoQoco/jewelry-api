using Microsoft.AspNetCore.Http;

namespace jewelry.Model.Stock.Product.Image.Replace
{
    public class Request
    {
        public string StockNumber { get; set; }
        public IFormFile Image { get; set; }
    }
}
