using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.List
{
    public class Request : DataSourceRequest
    {
        public Search Search { get; set; }
    }

    public class Search
    { 
        public int? Id { get; set; }
        public string? Username { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsNew { get; set; }
    }
}
