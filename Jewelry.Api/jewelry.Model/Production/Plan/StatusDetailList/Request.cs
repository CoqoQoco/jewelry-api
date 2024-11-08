using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Production.Plan.StatusDetailList
{
    public class Request : DataSourceRequest
    {
        public RequestSearch Search { get; set; }
    }

    public class RequestSearch
    {
        public DateTimeOffset? ReceivesDateStart { get; set; }
        public DateTimeOffset? ReceiveDateEnd { get; set; }

        public DateTimeOffset? ReceiveWorkDateStart { get; set; }
        public DateTimeOffset? ReceiveWorkDateEnd { get; set; }

        public int[]? Status { get; set; }
        public string[]? Gold { get; set; }

        public string? WoText { get; set; }
        public string? ProductNumber { get; set; }
    }
}
