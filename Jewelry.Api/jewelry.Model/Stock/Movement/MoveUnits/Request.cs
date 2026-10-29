namespace jewelry.Model.Stock.Movement.MoveUnits
{
    public class Request
    {
        public string TargetLocationCode { get; set; } = null!;
        public string? Remark { get; set; }
        public List<MoveUnitsItem> Items { get; set; } = new();

        // EPC ของแท็ก RFID (1 แท็ก = 1 ชิ้น) — ระบบหาชิ้นจากแท็กที่ผูกอยู่ แล้วย้ายเฉพาะชิ้นนั้น
        public List<string>? Epcs { get; set; }
    }

    // เลือกชิ้นด้วย UnitIds · หรือ FromLocationCode + Qty (n ชิ้นจากคลัง X) · ไม่ระบุทั้งคู่ = ย้ายทั้งล็อต
    public class MoveUnitsItem
    {
        public string StockNumber { get; set; } = null!;
        public List<long>? UnitIds { get; set; }
        public string? FromLocationCode { get; set; }
        public int? Qty { get; set; }
    }
}
