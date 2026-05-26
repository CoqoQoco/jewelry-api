namespace jewelry.Model.Stock.Balance.ByStockNumbers
{
    public class Response
    {
        public string StockNumber { get; set; }
        public string SkuCode { get; set; }
        public string LocationCode { get; set; }
        public string PieceStatus { get; set; }
        public decimal QtyOnHand { get; set; }
        public decimal QtyReserved { get; set; }
        public decimal QtyAvailable { get; set; }
    }
}
