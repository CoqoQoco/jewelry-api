using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Master.SearchMaster
{
    public class SearchMasterRequest : DataSourceRequest
    {
        public SearchMasterModel Search { get; set; }
    }
}
