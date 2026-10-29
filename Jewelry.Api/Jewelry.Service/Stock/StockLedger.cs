using System;

namespace Jewelry.Service.Stock
{
    // กติกา ledger กลาง: movement ไหนบวก/ลบ qty ของล็อต และ movement ไหนคือการย้ายสต็อกระบบเก่า
    public static class StockLedger
    {
        public const string LegacyImportRefPrefix = "TRANSFER_";
        public const string SilverTopUpRefDocType = "TRANSFER_SILVER_TOPUP";

        // +1 เข้า / -1 ออก / 0 ไม่กระทบ qty (reserve, unreserve, ย้ายคลัง)
        public static int QtySign(string? movementType)
        {
            switch (movementType?.Trim().ToUpperInvariant())
            {
                case "RECEIPT":
                case "RETURN":
                case "TRANSFORM_IN":
                    return 1;
                case "SALE":
                case "TRANSFORM_OUT":
                    return -1;
                default:
                    return 0;
            }
        }

        public static bool IsLegacyImportRef(string? refDocType)
            => refDocType?.StartsWith(LegacyImportRefPrefix, StringComparison.OrdinalIgnoreCase) ?? false;

        // รับของใหม่เข้าสต็อก = RECEIPT/TRANSFORM_IN ที่ไม่ใช่การย้ายสต็อกระบบเก่า (TRANSFER_*)
        public static bool IsNewStockReceipt(string? movementType, string? refDocType)
        {
            var type = movementType?.Trim().ToUpperInvariant();
            return (type == "RECEIPT" || type == "TRANSFORM_IN") && !IsLegacyImportRef(refDocType);
        }
    }
}
