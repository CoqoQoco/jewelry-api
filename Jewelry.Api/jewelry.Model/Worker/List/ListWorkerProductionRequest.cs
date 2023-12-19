using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Worker.List
{
    public class ListWorkerProductionRequest : DataSourceRequest
    {
        public ListWorkerProduction Search { get; set; }
    }
    public class ListWorkerProduction
    {
        public string? Text { get; set; }
        public int? Type { get; set; }
        public int? Active { get; set; }
    }
}
