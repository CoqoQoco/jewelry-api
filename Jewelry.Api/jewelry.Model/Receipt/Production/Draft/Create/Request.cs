using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.Draft.Create
{
    public class Request
    {
        //public int Id { get; set; }
        //public string Wo { get; set; }
        //public int WoNumber { get; set; }
        //public string WoText { get; set; }
        public string ReceiptNumber { get; set; }

        public List<Stock> Stocks { get; set; }
    }

    public class Stock
    {
        public string StockReceiptNumber { get; set; }
        public string? StockNumber { get; set; }


        public string? ProductNumber { get; set; }
        public string? ProductNameTH { get; set; }
        public string? ProductNameEN { get; set; }

        public decimal? Qty { get; set; }
        public decimal? Price { get; set; }

        public string? Size { get; set; }
        public string? Location { get; set; }

        public string? ImageName { get; set; }
        public int? ImageYear { get; set; }
        public string? ImagePath { get; set; }

        public string? Remark { get; set; }
        public bool? IsReceipt { get; set; }

        public List<Material> Materials { get; set; } = new List<Material>();
    }

    public class Material
    {
        public string? Type { get; set; }
        public string? SubType { get; set; }
        public string? Description { get; set; }

        public decimal? Qty { get; set; }
        public decimal? Weight { get; set; }

        public string? Size { get; set; }
        public decimal? Price { get; set; }
    }
}
