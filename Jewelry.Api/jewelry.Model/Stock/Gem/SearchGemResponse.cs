using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem
{
    public class SearchGemResponse
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }

        public string Size { get; set; }
        public string Shape { get; set; }
        public string Grade { get; set; }

        public decimal Price { get; set; }
    }
}
 