using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Master
{
    public class SearchMasterModelRequest
    {
       public SearchMasterModel Search { get; set; }
    }

    public class SearchMasterModel
    {
        public string Type { get; set; }
        public string? Text { get; set; }
    }
}
