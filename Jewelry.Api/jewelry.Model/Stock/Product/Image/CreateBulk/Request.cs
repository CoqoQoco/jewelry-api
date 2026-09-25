using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Image.CreateBulk
{
    public class Request
    {
        public IFormFile Image { get; set; }
        public List<string> StockNumbers { get; set; } = new List<string>();
        public bool Overwrite { get; set; }
        public string? Description { get; set; }
    }
}
