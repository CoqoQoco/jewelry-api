using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Mold
{
    public class ModelGem
    {
        public string GemCode { get; set; }
        public string? GemNameTH { get; set; }
        public string? GemNameEN { get; set; }

        public string GemShapeCode { get; set; }
        public string? GemShapeNameTH { get; set; }
        public string? GemShapeNameEN { get; set; }

        public string? Size { get; set; }
        public decimal? Qty { get; set; }
    }
}
