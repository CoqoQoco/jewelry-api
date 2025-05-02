using jewelry.Model.Receipt.Production.PlanList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.PlanGet
{
    public class Response
    {
        public int Id { get; set; }
        public string Wo { get; set; }
        public int WoNumber { get; set; }
        public string WoText { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }

        public string Mold { get; set; }

        public string? ProductRunning { get; set; }
        public string? ProductName { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductDetail { get; set; }
        public int ProductQty { get; set; }
        public string? ProductQtyUnit { get; set; }

        public string? CustomerNumber { get; set; }
        public string? CustomerName { get; set; }

        public string? CustomerType { get; set; }
        public string? CustomerTypeName { get; set; }

        public string? Remark { get; set; }

        public string? Gold { get; set; }
        public string? GoldSize { get; set; }

        public string ReceiptNumber { get; set; }
        public DateTime ReceiptDate { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsRunning { get; set; }
        public int QtyRunning { get; set; }

        public IEnumerable<Gem> Gems { get; set; }

        public List<ReceiptStock> Stocks { get; set; }
    }
    public class ReceiptStock
    {
        public string StockReceiptNumber { get; set; }
        public string? StockNumber { get; set; }


        public string? ProductNumber { get; set; }
        public string? ProductNameTH { get; set; }
        public string? ProductNameEN { get; set; }

        public string? MoldDesign { get; set; }

        public decimal? Qty { get; set; }
        public decimal? Price { get; set; }

        public string? Size { get; set; }
        public string? StudEarring { get; set; }
        public string? Location { get; set; }

        public string? ImageName { get; set; }
        public int? ImageYear { get; set; }
        public string? ImagePath { get; set; }

        public string? Remark { get; set; }
        public bool IsReceipt { get; set; }

        public List<Material> Materials { get; set; } = new List<Material>();
    }
    public class Material
    {
        public string? Type { get; set; }
        public string? TypeName { get; set; }
        public string? TypeCode { get; set; }
        public string? TypeBarcode { get; set; }

        public decimal? Qty { get; set; }
        public string? QtyUnit { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }

        public string? Size { get; set; }
        public string? Region { get; set; }
        public decimal? Price { get; set; }
    }

    public class Gem
    {
        public string GemCode { get; set; } 
        public decimal? GemQty { get; set; }
        public decimal? GemWeight { get; set; }
        public string? GemName { get; set; }
        public decimal? GemPrice { get; set; }
        public string? Remark { get; set; }
        public string? OutboundRunning { get; set; }
        public string? OutboundName { get; set; }
        public DateTime? OutboundDate { get; set; }
    }
}
