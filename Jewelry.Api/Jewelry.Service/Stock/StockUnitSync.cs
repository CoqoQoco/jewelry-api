using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Stock
{
    public class StockUnitSyncInfo
    {
        public string By { get; set; } = "system";
        public DateTime Now { get; set; } = DateTime.UtcNow;

        // ใบรับ/วันที่รับของ unit ที่สร้างใหม่ (เติมจำนวน) — ว่าง = ใช้ค่าจาก piece
        public string? ReceiptNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }

        // อ้างอิงที่ขาย/ใช้ unit (เลข invoice / เลข convert) และวันที่
        public string? SoldRef { get; set; }
        public DateTime? SoldDate { get; set; }

        // คืนของ: ถ้า qty เพิ่ม ให้ฟื้น unit ที่ SOLD ด้วย SoldRef นี้ก่อนสร้าง unit ใหม่
        public string? ReviveSoldRef { get; set; }

        // คลังของ unit ที่สร้างใหม่ — ว่าง = คลังหลักของล็อต (piece.LocationCode)
        public string? LocationCode { get; set; }
    }

    // ซิงก์จำนวน tbt_stock_unit (ชิ้นจริง) ให้ตรงกับ qty / qty_reserved ของล็อต (tbt_stock_piece) — "ตาข่ายนิรภัย" นับจำนวนเท่านั้น
    // - TK202610290003: unit เป็นความจริงรายคลัง (ล็อตเดียวอยู่ได้หลายคลัง) จึงไม่บังคับคลังของ unit ให้ตรงกับ piece.LocationCode อีกต่อไป
    //   flow ปกติ (จอง/ขาย/ย้าย/รับ) ใช้ StockUnitOps แทน — ReconcileAsync ใช้เฉพาะ self-heal ล็อตที่จำนวนไม่ตรง (เช่นล็อตที่ยังไม่มี unit)
    //   unit ใหม่ที่สร้างอยู่คลัง info.LocationCode ?? piece.LocationCode · ไม่แตะ balance/movement
    // - ต้องเรียกหลังแก้ค่าของ piece ใน transaction เดียวกับ SaveChanges ของผู้เรียก (helper นี้ไม่ SaveChanges เอง)
    // - ลำดับ lock คงเดิม: piece -> balance — unit ถูกแก้ภายใต้ lock ของ piece อยู่แล้ว ไม่ต้องล็อกเพิ่ม
    // - กติกาเลือกชิ้น: ชิ้นที่ยังไม่มีแท็กก่อน แล้วค่อย unit_no สูงสุด
    public static class StockUnitSync
    {
        public const string InStock = "IN_STOCK";
        public const string Reserved = "RESERVED";
        public const string Sold = "SOLD";

        public const string UnbindSold = "SOLD";
        public const string UnbindManual = "MANUAL";

        public static int ToUnits(decimal qty)
        {
            if (qty <= 0) return 0;
            return (int)Math.Floor(qty);
        }

        // ล็อตใหม่ที่เพิ่ง map (ยังไม่ save) — สร้าง unit ให้ครบตาม qty พร้อมกับ piece ใน SaveChanges เดียวกัน
        public static void AddUnitsForNewPieces(JewelryContext context, IEnumerable<TbtStockPiece> pieces, string by)
        {
            var now = DateTime.UtcNow;
            var newUnits = new List<TbtStockUnit>();

            foreach (var piece in pieces)
            {
                if (piece.Status == Sold) continue;

                var total = ToUnits(piece.Qty);
                var reserved = Math.Min(ToUnits(piece.QtyReserved), total);

                for (var n = 1; n <= total; n++)
                {
                    newUnits.Add(new TbtStockUnit
                    {
                        StockNumber = piece.StockNumber,
                        ProductCode = piece.ProductCode,
                        UnitNo = n,
                        LocationCode = piece.LocationCode,
                        Status = n <= reserved ? Reserved : InStock,
                        ReceiptNumber = piece.ReceiptNumber,
                        ReceiptDate = piece.ReceiptDate,
                        CreateDate = now,
                        CreateBy = by
                    });
                }
            }

            if (newUnits.Count > 0)
            {
                context.TbtStockUnit.AddRange(newUnits);
            }
        }

        public static async Task ReconcileAsync(JewelryContext context, TbtStockPiece piece, StockUnitSyncInfo info)
        {
            var stockNumber = piece.StockNumber;
            var productCode = piece.ProductCode;

            var units = await LoadUnitsAsync(context, stockNumber, productCode);
            var active = units.Where(u => u.Status != Sold).ToList();

            var targetQty = ToUnits(piece.Qty);
            var targetReserved = Math.Min(ToUnits(piece.QtyReserved), targetQty);
            var currentReserved = active.Count(u => u.Status == Reserved);

            if (active.Count == targetQty && currentReserved == targetReserved)
            {
                return;
            }

            var modified = new List<TbtStockUnit>();
            var added = new List<TbtStockUnit>();

            HashSet<long>? taggedIds = null;
            List<TbtStockPieceRfid>? activeTags = null;

            async Task<HashSet<long>> GetTaggedAsync()
            {
                if (taggedIds != null) return taggedIds;
                activeTags = await LoadActiveTagsAsync(context, stockNumber, productCode);
                taggedIds = activeTags.Where(t => t.UnitId.HasValue).Select(t => t.UnitId!.Value).ToHashSet();
                return taggedIds;
            }

            if (active.Count > targetQty)
            {
                var tagged = await GetTaggedAsync();
                var remove = active.Count - targetQty;

                // ขายชิ้นที่จองไว้ก่อน (reserved ลด) ตามจำนวนที่ยอดจองลดลง ที่เหลือเป็นชิ้นว่าง
                var soldReserved = Math.Clamp(currentReserved - targetReserved, Math.Max(0, remove - (active.Count - currentReserved)), Math.Min(remove, currentReserved));
                var soldFree = remove - soldReserved;

                var toSell = Pick(active.Where(u => u.Status == Reserved), soldReserved, tagged)
                    .Concat(Pick(active.Where(u => u.Status != Reserved), soldFree, tagged))
                    .ToList();

                foreach (var unit in toSell)
                {
                    unit.Status = Sold;
                    unit.SoldRef = info.SoldRef;
                    unit.SoldDate = info.SoldDate ?? info.Now;
                    Touch(unit, info);
                    modified.Add(unit);
                    active.Remove(unit);
                }

                foreach (var tag in activeTags!.Where(t => t.UnitId.HasValue && toSell.Any(u => u.Id == t.UnitId.Value && u.Id != 0)))
                {
                    tag.IsActive = false;
                    tag.UnbindReason = UnbindSold;
                    tag.UpdateDate = info.Now;
                    tag.UpdateBy = info.By;
                    context.TbtStockPieceRfid.Update(tag);
                }
            }
            else if (active.Count < targetQty)
            {
                var need = targetQty - active.Count;

                if (!string.IsNullOrEmpty(info.ReviveSoldRef))
                {
                    var revive = units
                        .Where(u => u.Status == Sold && u.SoldRef == info.ReviveSoldRef)
                        .OrderByDescending(u => u.UnitNo)
                        .Take(need)
                        .ToList();

                    foreach (var unit in revive)
                    {
                        unit.Status = InStock;
                        unit.SoldRef = null;
                        unit.SoldDate = null;
                        Touch(unit, info);
                        modified.Add(unit);
                        active.Add(unit);
                    }

                    need -= revive.Count;
                }

                var nextNo = units.Count == 0 ? 1 : units.Max(u => u.UnitNo) + 1;
                var receiptNumber = info.ReceiptNumber;
                var receiptDate = info.ReceiptDate;
                if (receiptNumber == null && receiptDate == null)
                {
                    receiptNumber = piece.ReceiptNumber;
                    receiptDate = piece.ReceiptDate;
                }

                for (var i = 0; i < need; i++)
                {
                    var unit = new TbtStockUnit
                    {
                        StockNumber = stockNumber,
                        ProductCode = productCode,
                        UnitNo = nextNo++,
                        LocationCode = info.LocationCode ?? piece.LocationCode,
                        Status = InStock,
                        ReceiptNumber = receiptNumber,
                        ReceiptDate = receiptDate,
                        CreateDate = info.Now,
                        CreateBy = info.By
                    };
                    added.Add(unit);
                    active.Add(unit);
                }
            }

            // ปรับจำนวน RESERVED ให้ตรง qty_reserved
            currentReserved = active.Count(u => u.Status == Reserved);
            if (currentReserved != targetReserved)
            {
                var tagged = await GetTaggedAsync();

                if (currentReserved < targetReserved)
                {
                    foreach (var unit in Pick(active.Where(u => u.Status == InStock), targetReserved - currentReserved, tagged))
                    {
                        unit.Status = Reserved;
                        Touch(unit, info);
                        if (unit.Id != 0) modified.Add(unit);
                    }
                }
                else
                {
                    foreach (var unit in Pick(active.Where(u => u.Status == Reserved), currentReserved - targetReserved, tagged))
                    {
                        unit.Status = InStock;
                        unit.ReservedRefType = null;
                        unit.ReservedRef = null;
                        unit.ReservedLineId = null;
                        Touch(unit, info);
                        if (unit.Id != 0) modified.Add(unit);
                    }
                }
            }

            var existing = modified.Where(u => u.Id != 0).Distinct().ToList();
            if (existing.Count > 0)
            {
                context.TbtStockUnit.UpdateRange(existing);
            }
            if (added.Count > 0)
            {
                context.TbtStockUnit.AddRange(added);
            }
        }

        // จำนวน unit ที่ไม่ SOLD / RESERVED ตรงกับ qty / qty_reserved ของล็อตหรือยัง
        public static bool CountsMatch(TbtStockPiece piece, IEnumerable<TbtStockUnit> units)
        {
            var active = units.Where(u => u.Status != Sold).ToList();
            var targetQty = ToUnits(piece.Qty);
            var targetReserved = Math.Min(ToUnits(piece.QtyReserved), targetQty);

            return active.Count == targetQty && active.Count(u => u.Status == Reserved) == targetReserved;
        }

        // คลังหลักของล็อต = คลังที่มีชิ้นไม่ SOLD มากสุด · เสมอกัน: คงคลังเดิมถ้าเป็นหนึ่งในที่มากสุด ไม่งั้นเลือกรหัสต่ำสุด
        // ไม่มีชิ้นเหลือเลย (ล็อตขายหมด) คงค่าเดิม — คืนค่า true เมื่อ piece.LocationCode เปลี่ยน
        public static bool RecalcPrimaryLocation(TbtStockPiece piece, IEnumerable<TbtStockUnit> units)
        {
            var groups = units
                .Where(u => u.Status != Sold && !string.IsNullOrEmpty(u.LocationCode))
                .GroupBy(u => u.LocationCode)
                .Select(g => new { Code = g.Key, Count = g.Count() })
                .ToList();

            if (groups.Count == 0) return false;

            var max = groups.Max(g => g.Count);
            var top = groups.Where(g => g.Count == max).Select(g => g.Code).ToList();

            var primary = top.Contains(piece.LocationCode)
                ? piece.LocationCode
                : top.OrderBy(c => c, StringComparer.Ordinal).First();

            if (primary == piece.LocationCode) return false;

            piece.LocationCode = primary;
            return true;
        }

        private static IEnumerable<TbtStockUnit> Pick(IEnumerable<TbtStockUnit> candidates, int count, HashSet<long> taggedIds)
        {
            if (count <= 0) return Enumerable.Empty<TbtStockUnit>();

            return candidates
                .OrderBy(u => u.Id != 0 && taggedIds.Contains(u.Id) ? 1 : 0)
                .ThenByDescending(u => u.UnitNo)
                .Take(count)
                .ToList();
        }

        private static void Touch(TbtStockUnit unit, StockUnitSyncInfo info)
        {
            unit.UpdateDate = info.Now;
            unit.UpdateBy = info.By;
        }

        public static async Task<List<TbtStockUnit>> LoadUnitsAsync(JewelryContext context, string stockNumber, string productCode)
        {
            var fromDb = await context.TbtStockUnit
                .Where(u => u.StockNumber == stockNumber && u.ProductCode == productCode)
                .ToListAsync();

            var fromLocal = context.TbtStockUnit.Local
                .Where(u => u.StockNumber == stockNumber && u.ProductCode == productCode);

            return fromDb.Union(fromLocal).ToList();
        }

        public static async Task<List<TbtStockPieceRfid>> LoadActiveTagsAsync(JewelryContext context, string stockNumber, string productCode)
        {
            var fromDb = await context.TbtStockPieceRfid
                .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode)
                .ToListAsync();

            var fromLocal = context.TbtStockPieceRfid.Local
                .Where(r => r.IsActive && r.StockNumber == stockNumber && r.ProductCode == productCode);

            return fromDb.Union(fromLocal).ToList();
        }
    }
}
