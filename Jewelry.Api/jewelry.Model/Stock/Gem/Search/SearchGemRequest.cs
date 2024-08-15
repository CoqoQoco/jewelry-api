using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem.Search
{
    public class SearchGemRequest : DataSourceRequest
    {
        public SearchGem Search { get; set; }
    }

    public class SearchGem
    {
        public int? Id { get; set; }

        public string? Code { get; set; }

        public string[]? GroupName { get; set; }
        public string[]? Size { get; set; }
        public string[]? Shape { get; set; }
        public string[]? Grade { get; set; }
    }
}
