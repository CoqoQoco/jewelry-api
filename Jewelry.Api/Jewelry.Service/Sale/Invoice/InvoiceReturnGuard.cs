using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Sale.Invoice
{
    // ตรวจก่อนคืนสต็อกตอนยกเลิกใบแจ้งหนี้ (TK202610290006) — คืนได้เฉพาะชิ้นที่ใบนี้ขายจริง และยังไม่เคยคืนมาก่อน
    // ใช้ ledger (movement) เป็นหลักฐาน: SALE/INVOICE ที่อ้างใบนี้ และ RETURN/INVOICE_DELETE ที่อ้างใบนี้
    public static class InvoiceReturnGuard
    {
        public const string SaleMovement = "SALE";
        public const string SaleRefDoc = "INVOICE";
        public const string ReturnMovement = "RETURN";
        public const string ReturnRefDoc = "INVOICE_DELETE";

        public static async Task<bool> CanReturnAsync(JewelryContext context, string invoiceNumber, TbtStockPiece piece)
        {
            var rows = await context.TbtStockMovement
                .Where(m => m.RefDocNo == invoiceNumber
                            && m.StockNumber == piece.StockNumber
                            && m.ProductCode == piece.ProductCode
                            && ((m.MovementType == SaleMovement && m.RefDocType == SaleRefDoc)
                                || (m.MovementType == ReturnMovement && m.RefDocType == ReturnRefDoc)))
                .Select(m => new { m.MovementType, m.Qty })
                .ToListAsync();

            var sold = rows.Where(r => r.MovementType == SaleMovement).Sum(r => r.Qty);
            var returned = rows.Where(r => r.MovementType == ReturnMovement).Sum(r => r.Qty);

            // เคยคืนครบแล้ว — ห้ามคืนซ้ำ
            if (sold > 0 && returned >= sold)
            {
                return false;
            }

            // มีหลักฐานการขายของใบนี้ และยังคืนไม่ครบ
            if (sold > 0)
            {
                return true;
            }

            // ไม่มี ledger (ข้อมูลก่อนมีระบบ movement) — ใช้ชิ้นรายตัวเป็นหลักฐาน
            var units = await StockUnitSync.LoadUnitsAsync(context, piece.StockNumber, piece.ProductCode);

            if (units.Any(u => u.Status == StockUnitSync.Sold && u.SoldRef == invoiceNumber))
            {
                return true;
            }

            // ล็อตนี้ถูกขายด้วยใบอื่น และไม่มีหลักฐานว่าใบนี้ขาย — ห้ามคืน (กันสร้างชิ้นผี)
            return !units.Any(u => u.Status == StockUnitSync.Sold && u.SoldRef != null);
        }
    }
}
