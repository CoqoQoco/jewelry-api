using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Base
{
    public partial class Response
    {
        public int Code { get; set; } = 200;
        public string Message { get; set; } = "success";
        public string? Error { get; set; } 
    }
}
