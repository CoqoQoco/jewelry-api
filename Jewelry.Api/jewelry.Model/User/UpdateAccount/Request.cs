using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.UpdateAccount
{
    public class Request
    {
        public int Id { get; set; }

        public string? ImageAction { get; set; }
        public IFormFile? Image { get; set; }
    }
}
