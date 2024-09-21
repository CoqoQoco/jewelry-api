using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem.Price
{
    public class PriceRequest : DataSourceRequest
    {
        public Price Search { get; set; }
    }

    public class Price
    {
        public string Code { get; set; }
    }
}
