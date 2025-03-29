using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Gem.List
{
    public class ListRequest : DataSourceRequest
    {
        public ListSearch Search { get; set; }
    }

    public class ListSearch
    {
        public DateTimeOffset? RequestDateStart { get; set; }
        public DateTimeOffset? RequestDateEnd { get; set; }

        public DateTimeOffset? ReturnDateStart { get; set; }
        public DateTimeOffset? ReturnDateEnd { get; set; }

        public string? Code { get; set; }
        public string[]? GroupName { get; set; }
        

        public int[]? Type { get; set; }
        public string? JobNoOrPONo { get; set; }
        public string? SupplierName { get; set; }

        public string[]? Size { get; set; }
        public string[]? Shape { get; set; }
        public string[]? Grade { get; set; }

        public string[]? Status { get; set; }

        public string? WO { get; set; }

        public string? Running { get; set; }
        public string? RefRunning1 { get; set; }
        public string? RefRunning2 { get; set; }

        public string? Operator { get; set; }
        public string? CreateBy { get; set; }
    }
}
