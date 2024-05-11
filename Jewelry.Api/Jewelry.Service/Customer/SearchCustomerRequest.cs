using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Customer
{
    public class SearchCustomerRequest : DataSourceRequest
    {
        public SearchCustomer Search { get; set; }
    }
    public class SearchCustomer
    {
        //[StringLength(30, MinimumLength = 4, ErrorMessage = "Maximum 30 characters")]
        public string? Text { get; set; }
    }
}
