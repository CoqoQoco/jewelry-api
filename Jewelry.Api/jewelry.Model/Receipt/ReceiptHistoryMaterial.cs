using System.Collections.Generic;

namespace jewelry.Model.Receipt
{
    public class ReceiptHistoryMaterial
    {
        public string? GoldType { get; set; }
        public decimal GoldWeight { get; set; }

        public decimal DiamondQty { get; set; }
        public decimal DiamondWeight { get; set; }

        public decimal RubyQty { get; set; }
        public decimal RubyWeight { get; set; }

        public decimal SapphireQty { get; set; }
        public decimal SapphireWeight { get; set; }

        public decimal EmeraldQty { get; set; }
        public decimal EmeraldWeight { get; set; }

        public decimal OtherQty { get; set; }
        public decimal OtherWeight { get; set; }

        public List<ReceiptHistoryMaterialLine>? Materials { get; set; }
    }

    public class ReceiptHistoryMaterialLine
    {
        public string Type { get; set; } = string.Empty;
        public string? TypeName { get; set; }
        public string? TypeCode { get; set; }
        public string? TypeOrigin { get; set; }
        public string? Size { get; set; }
        public decimal? Qty { get; set; }
        public string? QtyUnit { get; set; }
        public decimal? Weight { get; set; }
        public string? WeightUnit { get; set; }
        public string? Region { get; set; }
        public decimal? Price { get; set; }
        public decimal? PriceWeight { get; set; }
    }

    public class ReceiptHistorySummary
    {
        public int TotalQty { get; set; }
        public decimal TotalSales { get; set; }

        public decimal GoldWeight { get; set; }

        public decimal DiamondQty { get; set; }
        public decimal DiamondWeight { get; set; }

        public decimal RubyQty { get; set; }
        public decimal RubyWeight { get; set; }

        public decimal SapphireQty { get; set; }
        public decimal SapphireWeight { get; set; }

        public decimal EmeraldQty { get; set; }
        public decimal EmeraldWeight { get; set; }

        public decimal OtherQty { get; set; }
        public decimal OtherWeight { get; set; }
    }
}
