using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.Image.Create
{
    public class Request
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public IFormFile Image { get; set; }
    }
}
