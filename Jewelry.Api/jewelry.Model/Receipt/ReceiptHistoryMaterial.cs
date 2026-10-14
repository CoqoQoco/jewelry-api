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
