using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.Image.Create
{
    public class Request
    {
        public string Name { get; set; }
        public List<IFormFile> Images { get; set; }
    }
}
