using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.PlanBOM.List
{
    public class Request : DataSourceRequest
    {
        public Criteria Search { get; set; }
    }

    public class Criteria
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        public string? Text { get; set; }
        public string? WoText { get; set; }

        public string[]? CustomerType { get; set; }
        public string? CustomerCode { get; set; }

        public string[]? Gold { get; set; }
        public string[]? GoldSize { get; set; }

        public string? Mold { get; set; }
        public string? ProductNumber { get; set; }
        public string[]? ProductType { get; set; }
    }
}
