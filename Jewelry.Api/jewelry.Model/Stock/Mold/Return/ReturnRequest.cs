using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Mold.Return
{
    public class ReturnRequest
    {
        public int Id { get; set; }
        public string Mold { get; set; }

        public DateTimeOffset ReturnDate { get; set; }
        public string ReturnName { get; set; }
        public string? ReturnDesc { get; set; }

    }
}
