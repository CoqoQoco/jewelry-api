using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Customer
{
    public class SearchCustomerResponse 
    {
        public string Code { get; set; }
        public string NameTh { get; set; }

        public string? NameEn { get; set; }

        public string? Address { get; set; }

        public string TypeCode { get; set; }
        public string TypeName { get; set; }

        public string? Telephone1 { get; set; }

        public string? Telephone2 { get; set; }

        public string? ContactName { get; set; }

        public string? Email { get; set; }

        public string? Remark { get; set; }

        public int OrderCount { get; set; }
    }

  
}
