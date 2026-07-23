using System;
using System.Collections.Generic;

namespace jewelry.Model.Worker.WagesByProcess
{
    public class SearchRequest
    {
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public List<int>? Status { get; set; }
    }
}
