using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Stock
{
    public class StockMovementTemplate
    {
        public string MovementType { get; set; } = null!;
        public string? RefDocType { get; set; }
        public string? RefDocNo { get; set; }
        public string? Remark { get; set; }
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string By { get; set; } = "system";
    }

    // เขียน balance (SKU x คลัง) + movement ต่อคลังจากผลของ StockUnitOps — ใช้แทนโค้ดเขียนมือที่อ้าง piece.LocationCode
    // ผู้เรียกต้องล็อก piece -> balance ของ SKU ไว้ก่อนแล้ว และ SaveChanges เอง
    public static class StockBalanceApplier
    {
        // writeBalance = false: เขียนเฉพาะ movement (ผู้เรียกจัดการ balance เองแล้ว เช่น ProductService แยก SKU)
        public static async Task ApplyAsync(JewelryContext context, UnitOpResult result, StockMovementTemplate template, bool writeBalance = true)
        {
            if (result.Units.Count == 0)
            {
                return;
            }

            var piece = result.Piece;

            foreach (var delta in writeBalance ? result.Deltas : new List<UnitDelta>())
            {
                if (delta.OnHand == 0 && delta.Reserved == 0) continue;

                var balance = await FindBalanceAsync(context, result.SkuCode, delta.Location);

                if (balance == null)
                {
                    // ไม่มีแถว balance: สร้างเฉพาะตอนมีของเข้าคลังนั้น ส่วนการตัด/จองที่ไม่มีแถวให้ข้าม (ไม่สร้างยอดติดลบ)
                    if (delta.OnHand <= 0) continue;

                    context.TbtStockBalance.Add(new TbtStockBalance
                    {
                        SkuCode = result.SkuCode,
                        LocationCode = delta.Location,
                        QtyOnHand = delta.OnHand,
                        QtyReserved = delta.Reserved,
                        LastMovementAt = template.Date,
                        CreateDate = template.Date,
                        CreateBy = template.By
                    });
                    continue;
                }

                balance.QtyOnHand += delta.OnHand;
                balance.QtyReserved += delta.Reserved;
                balance.LastMovementAt = template.Date;
                balance.UpdateDate = template.Date;
                balance.UpdateBy = template.By;
                if (context.Entry(balance).State != EntityState.Added)
                {
                    context.TbtStockBalance.Update(balance);
                }
            }

            foreach (var line in result.Lines)
            {
                var movement = new TbtStockMovement
                {
                    MovementDate = template.Date,
                    MovementType = template.MovementType,
                    SkuCode = result.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = line.From,
                    ToLocation = line.To,
                    Qty = line.Qty,
                    RefDocType = template.RefDocType,
                    RefDocNo = template.RefDocNo,
                    Remark = template.Remark,
                    CreateDate = template.Date,
                    CreateBy = template.By
                };
                context.TbtStockMovement.Add(movement);

                foreach (var unit in line.Units)
                {
                    context.TbtStockMovementUnit.Add(new TbtStockMovementUnit
                    {
                        Movement = movement,
                        Unit = unit
                    });
                }
            }
        }

        private static async Task<TbtStockBalance?> FindBalanceAsync(JewelryContext context, string skuCode, string location)
        {
            var local = context.TbtStockBalance.Local
                .FirstOrDefault(b => b.SkuCode == skuCode && b.LocationCode == location);
            if (local != null)
            {
                return local;
            }

            return await context.TbtStockBalance
                .FirstOrDefaultAsync(b => b.SkuCode == skuCode && b.LocationCode == location);
        }
    }
}
