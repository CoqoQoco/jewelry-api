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

        // ล็อตเดียวหลายคลัง: LocationCode ด้านบน = คลังหลัก (มีชิ้นมากสุด) · Locations = จำนวนชิ้นของล็อตนี้ในแต่ละคลัง (จาก unit ที่ไม่ SOLD)
        // พร้อมยอด balance (SKU x คลัง) ของแต่ละคลัง
        public List<LocationBalance> Locations { get; set; } = new();
        public int LocationCount { get; set; }
    }

    public class LocationBalance
    {
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public decimal Qty { get; set; }
        public decimal Reserved { get; set; }
        public decimal Available { get; set; }
        public decimal BalanceOnHand { get; set; }
        public decimal BalanceReserved { get; set; }
        public decimal BalanceAvailable { get; set; }
    }
}
