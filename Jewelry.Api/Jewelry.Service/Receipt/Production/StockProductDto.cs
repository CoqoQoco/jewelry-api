using System;

namespace Jewelry.Service.Receipt.Production
{
    public class StockProductDto
    {
        public string StockNumber { get; set; } = null!;
        public string? Status { get; set; }
        public string ReceiptNumber { get; set; } = null!;
        public DateTime ReceiptDate { get; set; }
        public string ReceiptType { get; set; } = null!;
        public string? Mold { get; set; }
        public string? MoldDesign { get; set; }
        public decimal Qty { get; set; }
        public decimal ProductPrice { get; set; }
        public string ProductNumber { get; set; } = null!;
        public string ProductNameTh { get; set; } = null!;
        public string ProductNameEn { get; set; } = null!;
        public string ProductType { get; set; } = null!;
        public string? ProductTypeName { get; set; }
        public string? ImageName { get; set; }
        public string? ImagePath { get; set; }
        public string? Wo { get; set; }
        public int? WoNumber { get; set; }
        public DateTime ProductionDate { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? Size { get; set; }
        public string? StudEarring { get; set; }
        public string? Location { get; set; }
        public string? Remark { get; set; }
        public string? ProductCode { get; set; }
        public string? WoOrigin { get; set; }
        public decimal? ProductCost { get; set; }
        public string? ProductCostDetail { get; set; }
        public decimal? TagPriceMultiplier { get; set; }
        public string CreateBy { get; set; } = null!;
        public DateTime CreateDate { get; set; }
    }
}
