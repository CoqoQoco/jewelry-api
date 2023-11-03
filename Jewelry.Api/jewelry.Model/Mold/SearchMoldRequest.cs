using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold
{
    public class SearchMoldRequest
    {
        public SearchMold Search{ get; set; }
    }

    public class SearchMold
    {
        public string? Text { get; set; }
    }
}
