using System;
using System.Collections.Generic;

namespace jewelry.Model.Production.Plan.LeadTimeReport
{
    public class SearchRequest
    {
        public DateTimeOffset? CompletedStart { get; set; }
        public DateTimeOffset? CompletedEnd { get; set; }
        public string GroupBy { get; set; } = "productType";
        public List<string>? ProductType { get; set; }
        public List<string>? CustomerType { get; set; }
    }
}
