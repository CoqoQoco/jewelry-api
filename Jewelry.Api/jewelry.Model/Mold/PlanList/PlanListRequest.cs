using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold.PlanList
{
    public class PlanListRequest : DataSourceRequest
    {
        public PlanListRequestModel Search { get; set; }
    }

    public class PlanListRequestModel
    {
        public string? MoldCode { get; set; }

        public DateTimeOffset? CreateStart { get; set; }
        public DateTimeOffset? CreateEnd { get; set; }

        public DateTimeOffset? UpdateStart { get; set; }
        public DateTimeOffset? UpdateEnd { get; set; }
    }
}
