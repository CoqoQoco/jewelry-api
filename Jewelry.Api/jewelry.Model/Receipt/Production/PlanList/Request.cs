using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.PlanList
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    {
        public string? WO { get; set; }
    }
}
