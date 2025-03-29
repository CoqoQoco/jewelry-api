using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.Picklist
{
    public class PicklistRequest : DataSourceRequest
    {
        public PicklistFilter Search { get; set; }
    }

    public class PicklistFilter
    {
        public string? Running { get; set; }
        public string? Code { get; set; }
        public int[]? Type { get; set; }
        public string[]? Status { get; set; }

        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }

        public DateTimeOffset? ReturnDateStart { get; set; }
        public DateTimeOffset? ReturnDateEnd { get; set; }

        public string? GetRunning { get; set; }

        public string? Operator { get; set; }
        public string? CreateBy { get; set; }
    }
}
