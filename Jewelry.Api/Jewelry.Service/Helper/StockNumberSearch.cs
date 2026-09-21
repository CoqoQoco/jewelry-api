namespace Jewelry.Service.Helper
{
    // เลขเก่ามีขีด / เลขใหม่ไม่มีขีด (ตั้งแต่ 2026-09) ค้นต้องตัดขีดทั้ง 2 ฝั่ง
    // ในฝั่ง DB ใช้ x.StockNumber.Replace("-", "") ซึ่ง Npgsql แปลงเป็น SQL replace() ได้ (อย่าเรียก Normalize() ข้างใน lambda)
    public static class StockNumberSearch
    {
        public static string Normalize(string? input) => (input ?? string.Empty).Trim().Replace("-", "").ToUpperInvariant();
    }
}
