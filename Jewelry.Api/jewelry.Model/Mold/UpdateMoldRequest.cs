using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold
{
    public class UpdateMoldRequest
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string CategoryCode { get; set; }
        public IFormFile? Images { get; set; }
    }
}
