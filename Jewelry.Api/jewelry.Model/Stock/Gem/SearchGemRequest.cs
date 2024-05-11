using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem
{
    public class SearchGemRequest : DataSourceRequest
    {
        public SearchGem Search { get; set; }
    }

    public class SearchGem
    {
        public string? Text { get; set; }
        public int? Id { get; set; }
    }
}
