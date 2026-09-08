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

        // ยอดของ piece (ล็อต) นี้โดยเฉพาะ ต่างจาก QtyOnHand/QtyReserved/QtyAvailable ด้านบนซึ่งเป็นยอดรวมทั้ง SKU x คลัง
        public decimal PieceQty { get; set; }
        public decimal PieceQtyReserved { get; set; }
        public decimal PieceQtyAvailable { get; set; }
    }
}
