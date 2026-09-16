namespace jewelry.Model.Stock.Product.Availability
{
    public class Response
    {
        public string StockNumber { get; set; } = null!;
        public string? StockNumberOrigin { get; set; }
        public string ProductCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public decimal Qty { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal QtyAvailable { get; set; }
        public string? Status { get; set; }
    }
}
