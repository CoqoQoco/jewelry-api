using System;
using System.Collections.Generic;

namespace jewelry.Model.Stock.Product.Dashboard
{
    public class DashboardRequest
    {
        public DateTimeOffset? StartDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public List<string>? ProductType { get; set; }
        public List<string>? ProductionType { get; set; }
        public List<string>? ProductionTypeSize { get; set; }
        public string? Status { get; set; }
    }
}
