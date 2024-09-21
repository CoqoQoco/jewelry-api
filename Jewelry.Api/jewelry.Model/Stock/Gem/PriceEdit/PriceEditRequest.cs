using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Gem.PriceEdit
{
    public class PriceEditRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }

       // public decimal PreviousPrice { get; set; }
        public decimal NewPrice { get; set; }

        //public decimal PreviousPriceUnit { get; set; }
        public decimal NewPriceUnit { get; set; }

        public string Unit { get; set; }
        public string? UnitCode { get; set; }

        public string? Pass { get; set; }
    }
}
