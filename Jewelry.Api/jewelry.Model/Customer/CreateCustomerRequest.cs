using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Customer
{
    public class CreateCustomerRequest
    {
        public string Code { get; set; }

        public string NameTH { get; set; }
        public string? NameEN { get; set; }

        public string? Address { get; set; }
        public string Type { get; set; }

        public string? Tel1 { get; set; }
        public string? Tel2 { get; set; }

        public string? Email { get; set; }
        public string? ContactName { get; set; }
        public string? Remark { get; set; }
    }
}
