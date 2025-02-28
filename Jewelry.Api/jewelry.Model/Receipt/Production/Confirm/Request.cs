using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.Confirm
{
    public class Request
    {
        public string ReceiptNumber { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }

        public List<ConfirmStock> Stocks { get; set; }
    }

    public class ConfirmStock
    {
        public string StockReceiptNumber { get; set; }

        public string ProductNumber { get; set; }
        public string ProductNameTH { get; set; }
        public string ProductNameEN { get; set; }

        public decimal Qty { get; set; }
        public decimal Price { get; set; }

        public string? Size { get; set; }
        public string? Location { get; set; }

        public string? ImageName { get; set; }
        public int? ImageYear { get; set; }
        public string? ImagePath { get; set; }

        public string? Remark { get; set; }

        public List<ConfirmMaterial> Materials { get; set; } = new List<ConfirmMaterial>();
    }

    public class ConfirmMaterial
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
