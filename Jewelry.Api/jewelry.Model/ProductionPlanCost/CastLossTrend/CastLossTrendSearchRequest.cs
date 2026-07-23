using System;
using System.Collections.Generic;

namespace jewelry.Model.ProductionPlanCost.CastLossTrend
{
    public class SearchRequest
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public List<string>? Gold { get; set; }
    }
}
