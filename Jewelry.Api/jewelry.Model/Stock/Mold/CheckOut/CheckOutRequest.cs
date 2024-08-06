using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Mold.CheckOut
{
    public class CheckOutRequest
    {
        public string Mold { get; set; }

        public DateTimeOffset CheckOutDate { get; set; }
        public string CheckOutName { get; set; }
        public string CheckOutDesc { get; set; }

        public DateTimeOffset ReturnOutDate { get; set; }
    }
}
