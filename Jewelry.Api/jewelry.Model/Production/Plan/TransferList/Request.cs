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
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public string? TransferNumber { get; set; }
        public string? WoText { get; set; }

        public int? StatusFormer { get; set; }
        public int? StatusTarget { get; set; }

        public string[]? Gold { get; set; }
        public string[]? GoldSize { get; set; }

        public string? Mold { get; set; }

        public string? ProductNumber { get; set; }
        public string[]? ProductType { get; set; }
    }
}
