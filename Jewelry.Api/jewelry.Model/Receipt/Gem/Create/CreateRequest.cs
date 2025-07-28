using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Create
{
    public class CreateRequest
    {
        public string Code { get; set; }
        public string GroupName { get; set; }

        public string Size { get; set; }
        public string Shape { get; set; }
        public string Grade { get; set; }
        public string GradeCode { get; set; }

        public string Remark { get; set; }

        public string? Region { get; set; }
    }
}
