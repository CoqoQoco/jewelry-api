using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Stock.Product.Update
{
    public class Request
    {
        public string StockNumber { get; set; }
        public string ReceiptNumber { get; set; }

        public string ProductNameEn { get; set; }
        public string ProductNameTh { get; set; }

        public string Mold { get; set; }
        public decimal ProductPrice { get; set; }

        public decimal Qty { get; set; }
        public string? Size { get; set; }
        public string? Location { get; set; }

        public string? ImageName { get; set; }
        public string? ImagePath { get; set; }

        public List<Material> Materials { get; set; } = new List<Material>();
    }

    public class Material
    {
        public string Type { get; set; }
        public string? TypeName { get; set; }
        public string? TypeCode { get; set; }
        public string? TypeBarcode { get; set; }

        public decimal? Qty { get; set; }
        public string? QtyUnit { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }

        public string? Size { get; set; }
        public decimal? Price { get; set; }
    }
}
