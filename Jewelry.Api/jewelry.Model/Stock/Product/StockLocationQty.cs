namespace jewelry.Model.Stock.Product
{
    // จำนวนชิ้นของล็อตในแต่ละคลัง (นับจาก tbt_stock_unit ที่ไม่ SOLD)
    public class StockLocationQty
    {
        public string Code { get; set; } = null!;
        public string? Name { get; set; }
        public decimal Qty { get; set; }
        public decimal Reserved { get; set; }
        public decimal Available { get; set; }
    }
}
