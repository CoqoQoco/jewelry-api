using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.Plan.TransferList
{
    public class Request : DataSourceRequest
    {
        public RequestSearch Search { get; set; }
    }

    public class RequestSearch
    { 
        public string? TransferNumber { get; set; }
    }
}
