using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.Plan.Receipt.List
{
    public class Request : DataSourceRequest
    {
        public RequestSearch Search { get; set; }
    }
    public class RequestSearch
    { 
        public string? Running { get; set; }
    }
}
