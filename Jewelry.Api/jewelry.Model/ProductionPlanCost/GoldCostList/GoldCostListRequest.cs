﻿using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.ProductionPlanCost.GoldCostList
{
    public class GoldCostListRequest : DataSourceRequest
    {
        public GoldCostList Search { get; set; }
    }

    public class GoldCostList
    {
        public string? Text { get; set; }
        public string? RunningNumber { get; set; }
        public DateTimeOffset? CreateStart { get; set; }
        public DateTimeOffset? CreateEnd { get; set; }

        public string? BookNo { get; set; }
        public string? No { get; set; }
    }
}
