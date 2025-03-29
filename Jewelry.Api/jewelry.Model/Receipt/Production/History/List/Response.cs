using Kendo.DynamicLinqCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.Receipt.Production.History.List
{
    public class Response
    {
        public string StockNumber { get; set; }
        public string ReceiptNumber { get; set; }
        public string ReceiptType { get; set; }
        public DateTime ReceiptDate { get; set; }

        public string ProductNumber { get; set; }
        public string ProductNameEn { get; set; }
        public string ProductNameTh { get; set; }

        public string? ProductTypeName { get; set; }
        public string ProductType { get; set; }
        public decimal ProductPrice { get; set; }

        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public string? WoText { get; set; }

        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public DateTime ProductionDate { get; set; }

        public string? Mold { get; set; }
        public string? MoldDesign { get; set; }


        public string? ImageName { get; set; }
        public string? ImagePath { get; set; }

        public string? Status { get; set; }
        public decimal Qty { get; set; }

        public string? Location { get; set; }
        public string? Size { get; set; }

        public string? StudEarring { get; set; }

        public string? Remark { get; set; }

        public DateTime CreateDate { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime? UpdateDate { get; set; }
        public string? UpdateBy { get; set; }
    }

   
}
