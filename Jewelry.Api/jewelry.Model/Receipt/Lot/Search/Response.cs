using System;

namespace jewelry.Model.Receipt.Lot.Search
{
    public class Response
    {
        public string StockNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }

        public string? ProductNumber { get; set; }
        public string? ProductNameTh { get; set; }
        public string? ProductNameEn { get; set; }

        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? Size { get; set; }

        public string LocationCode { get; set; } = null!;

        public decimal Qty { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal QtyAvailable { get; set; }
        public string? Status { get; set; }

        public DateTime? ReceiptDate { get; set; }
        public string? ImagePath { get; set; }
    }
}
