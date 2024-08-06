using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Mold.CheckOutList
{
    public class CheckOutListRequest : DataSourceRequest
    {
        public CheckOutList Search { get; set; }
    }

    public class CheckOutList 
    { 
        public string? Text { get; set; }

        public DateTimeOffset? CheckOutDateStart { get; set; }
        public DateTimeOffset? CheckOutDateEnd { get; set; }

        public DateTimeOffset? ReturnDateStart { get; set; }
        public DateTimeOffset? ReturnDateEnd { get; set; }
    }
}
