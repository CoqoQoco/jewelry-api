using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Stock
{
    // ผลต่างยอดรายคลัง (balance) ที่เกิดจาก 1 operation ต่อ 1 ล็อต
    public sealed class UnitDelta
    {
        public string Location { get; set; } = null!;
        public decimal OnHand { get; set; }
        public decimal Reserved { get; set; }
    }

    // 1 แถว movement = (ล็อต, จากคลัง, ไปคลัง) พร้อมชิ้นที่เกี่ยวข้อง
    public sealed class UnitMovementLine
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public List<TbtStockUnit> Units { get; } = new();
        public int Qty => Units.Count;
    }

    public sealed class UnitOpResult
    {
        public UnitOpResult(TbtStockPiece piece)
        {
            Piece = piece;
            SkuCode = piece.SkuCode;
        }

        public TbtStockPiece Piece { get; }

        // SKU ที่ใช้เขียน balance/movement — ปกติ = piece.SkuCode ณ ตอนสร้างผล (ProductService fork SKU ตั้งค่าเองได้)
        public string SkuCode { get; set; }

        public List<TbtStockUnit> Units { get; } = new();
        public List<UnitDelta> Deltas { get; } = new();
        public List<UnitMovementLine> Lines { get; } = new();

        public int Count => Units.Count;

        internal void AddDelta(string location, decimal onHand, decimal reserved)
        {
            var delta = Deltas.FirstOrDefault(d => d.Location == location);
            if (delta == null)
            {
                delta = new UnitDelta { Location = location };
                Deltas.Add(delta);
            }
            delta.OnHand += onHand;
            delta.Reserved += reserved;
        }

        internal void AddLine(string? from, string? to, TbtStockUnit unit)
        {
            var line = Lines.FirstOrDefault(l => l.From == from && l.To == to);
            if (line == null)
            {
                line = new UnitMovementLine { From = from, To = to };
                Lines.Add(line);
            }
            line.Units.Add(unit);
            if (!Units.Contains(unit)) Units.Add(unit);
        }
    }

    // อ้างอิงการจอง: SO (เลข SO + id บรรทัด tbt_sale_order_product) หรือ CONVERT (เลขใบแปลง)
    public readonly record struct ReservationKey(string RefType, string Ref, long? LineId)
    {
        public const string So = "SO";
        public const string Convert = "CONVERT";

        public bool Matches(TbtStockUnit unit)
        {
            if (unit.ReservedRefType != RefType) return false;
            return LineId.HasValue ? unit.ReservedLineId == LineId : unit.ReservedRef == Ref;
        }
    }

    // Operation รายชิ้นของล็อต (tbt_stock_unit) — TK202610290003
    // - ไม่ล็อกและไม่ SaveChanges เอง: ผู้เรียกต้องล็อก piece -> balance ตามลำดับ global ก่อน แล้ว SaveChanges ใน transaction เดียวกัน
    // - แก้ทั้ง unit และ piece (qty / qty_reserved / status / คลังหลัก) และคืนผลต่างรายคลัง (Deltas) + แถว movement (Lines)
    //   ให้ StockBalanceApplier เขียน balance/movement ต่อคลังให้ถูกต้อง
    public static class StockUnitOps
    {
        // โหลด unit ของล็อต (รวมที่ยังไม่ save) — ถ้าจำนวนไม่ตรงกับ piece ซิงก์จำนวนก่อน (ล็อตที่ยังไม่มี unit ฯลฯ)
        public static async Task<List<TbtStockUnit>> LoadLotAsync(JewelryContext context, TbtStockPiece piece, StockUnitSyncInfo info)
        {
            var units = await StockUnitSync.LoadUnitsAsync(context, piece.StockNumber, piece.ProductCode);

            if (!StockUnitSync.CountsMatch(piece, units))
            {
                await StockUnitSync.ReconcileAsync(context, piece, info);
                units = await StockUnitSync.LoadUnitsAsync(context, piece.StockNumber, piece.ProductCode);
            }

            return units;
        }

        #region *** Reserve ***

        public static async Task<UnitOpResult> ReserveAsync(
            JewelryContext context,
            TbtStockPiece piece,
            int qty,
            IReadOnlyList<long>? unitIds,
            string? locationCode,
            ReservationKey key,
            StockUnitSyncInfo info)
        {
            if (qty <= 0)
            {
                throw new HandleException("จำนวนที่จองต้องมากกว่า 0");
            }

            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);
            var location = NormalizeLocation(locationCode);
            var picked = new List<TbtStockUnit>();

            if (unitIds != null && unitIds.Count > 0)
            {
                if (unitIds.Distinct().Count() != unitIds.Count)
                {
                    throw new HandleException($"ระบุชิ้นซ้ำกันในสินค้า {piece.StockNumber}");
                }

                if (unitIds.Count != qty)
                {
                    throw new HandleException($"สินค้า {piece.StockNumber}: จำนวนชิ้นที่เลือก ({unitIds.Count}) ไม่ตรงกับจำนวนที่ขอ ({qty})");
                }

                foreach (var id in unitIds)
                {
                    var unit = units.FirstOrDefault(u => u.Id == id);
                    if (unit == null)
                    {
                        throw new HandleException($"ชิ้นที่เลือกไม่อยู่ในสินค้า {piece.StockNumber}");
                    }

                    if (unit.Status == StockUnitSync.Sold)
                    {
                        throw new HandleException($"สินค้า {piece.StockNumber} ชิ้นที่ {unit.UnitNo} ขายแล้ว");
                    }

                    if (location != null && !SameLocation(unit.LocationCode, location))
                    {
                        throw new HandleException($"สินค้า {piece.StockNumber} ชิ้นที่ {unit.UnitNo} ไม่ได้อยู่คลัง {location} (อยู่คลัง {unit.LocationCode})");
                    }

                    if (unit.Status == StockUnitSync.Reserved)
                    {
                        if (key.Matches(unit))
                        {
                            picked.Add(unit);
                            continue;
                        }

                        throw new HandleException($"สินค้า {piece.StockNumber} ชิ้นที่ {unit.UnitNo} ถูกจองให้รายการอื่นแล้ว");
                    }

                    picked.Add(unit);
                }
            }
            else
            {
                var free = units.Where(u => u.Status == StockUnitSync.InStock).ToList();
                var tagged = await TaggedIdsAsync(context, piece);

                if (location != null)
                {
                    var atLocation = free.Where(u => SameLocation(u.LocationCode, location)).ToList();
                    if (atLocation.Count < qty)
                    {
                        throw new HandleException($"สินค้า {piece.StockNumber} พร้อมขายที่คลัง {location} {atLocation.Count} ชิ้น แต่ขอ {qty} ชิ้น");
                    }

                    picked.AddRange(Pick(atLocation, qty, tagged));
                }
                else
                {
                    if (free.Count < qty)
                    {
                        throw new HandleException($"สินค้า {piece.StockNumber} พร้อมขาย {free.Count} ชิ้น แต่ขอ {qty} ชิ้น");
                    }

                    var remaining = qty;
                    var groups = free
                        .GroupBy(u => u.LocationCode)
                        .OrderBy(g => g.Key == piece.LocationCode ? 0 : 1)
                        .ThenByDescending(g => g.Count())
                        .ThenBy(g => g.Key, StringComparer.Ordinal);

                    foreach (var group in groups)
                    {
                        if (remaining <= 0) break;
                        var take = Math.Min(remaining, group.Count());
                        picked.AddRange(Pick(group, take, tagged));
                        remaining -= take;
                    }
                }
            }

            var newlyReserved = 0;
            foreach (var unit in picked)
            {
                if (unit.Status == StockUnitSync.Reserved)
                {
                    continue;
                }

                unit.Status = StockUnitSync.Reserved;
                unit.ReservedRefType = key.RefType;
                unit.ReservedRef = key.Ref;
                unit.ReservedLineId = key.LineId;
                Touch(unit, info);

                result.AddDelta(unit.LocationCode, 0, 1);
                result.AddLine(unit.LocationCode, null, unit);
                newlyReserved++;
            }

            piece.QtyReserved += newlyReserved;
            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** Release (unreserve) ***

        public static async Task<UnitOpResult> ReleaseAsync(
            JewelryContext context,
            TbtStockPiece piece,
            ReservationKey key,
            decimal legacyQty,
            StockUnitSyncInfo info)
        {
            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);

            var linked = units.Where(u => u.Status == StockUnitSync.Reserved && key.Matches(u)).ToList();
            var expected = StockUnitSync.ToUnits(legacyQty);

            if (linked.Count < expected)
            {
                linked.AddRange(await PickLegacyReservedAsync(context, piece, units, expected - linked.Count));
            }

            foreach (var unit in linked)
            {
                unit.Status = StockUnitSync.InStock;
                unit.ReservedRefType = null;
                unit.ReservedRef = null;
                unit.ReservedLineId = null;
                Touch(unit, info);

                result.AddDelta(unit.LocationCode, 0, -1);
                result.AddLine(null, unit.LocationCode, unit);
            }

            piece.QtyReserved = Math.Max(0, piece.QtyReserved - linked.Count);
            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** Sell / consume reserved units ***

        // ขาย (invoice) หรือใช้ (convert) ชิ้นที่จองไว้ — ชิ้นที่ผูก key นี้เท่านั้น
        // ใบจองเก่าที่ยังไม่มีชิ้นผูก: fallback ไปใช้ชิ้น RESERVED ที่ไม่มี line (คลังหลักก่อน) ถ้ายังไม่พอถือว่าข้อมูลการจองผิดปกติ
        public static async Task<UnitOpResult> SellReservedAsync(
            JewelryContext context,
            TbtStockPiece piece,
            ReservationKey key,
            decimal qty,
            string soldRef,
            DateTime soldDate,
            StockUnitSyncInfo info)
        {
            var count = StockUnitSync.ToUnits(qty);
            if (count <= 0)
            {
                throw new HandleException("จำนวนที่ขายต้องมากกว่า 0");
            }

            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);
            var tagged = await TaggedIdsAsync(context, piece);

            var linked = Pick(units.Where(u => u.Status == StockUnitSync.Reserved && key.Matches(u)), count, tagged).ToList();

            if (linked.Count < count)
            {
                linked.AddRange(await PickLegacyReservedAsync(context, piece, units, count - linked.Count));
            }

            if (linked.Count < count)
            {
                throw new HandleException($"สินค้า {piece.StockNumber} มีชิ้นที่จองไว้ให้รายการนี้ {linked.Count} ชิ้น แต่ต้องขาย {count} ชิ้น กรุณาตรวจสอบการจองก่อนทำรายการ");
            }

            var activeTags = await StockUnitSync.LoadActiveTagsAsync(context, piece.StockNumber, piece.ProductCode);

            foreach (var unit in linked)
            {
                unit.Status = StockUnitSync.Sold;
                unit.SoldRef = soldRef;
                unit.SoldDate = soldDate;
                Touch(unit, info);

                result.AddDelta(unit.LocationCode, -1, -1);
                result.AddLine(unit.LocationCode, null, unit);

                foreach (var tag in activeTags.Where(t => t.UnitId == unit.Id && unit.Id != 0))
                {
                    tag.IsActive = false;
                    tag.UnbindReason = StockUnitSync.UnbindSold;
                    tag.UpdateDate = info.Now;
                    tag.UpdateBy = info.By;
                    context.TbtStockPieceRfid.Update(tag);
                }
            }

            piece.Qty -= linked.Count;
            piece.QtyReserved = Math.Max(0, piece.QtyReserved - linked.Count);
            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** Revive (invoice deleted) ***

        // คืนชิ้นที่ขายด้วย soldRef กลับเป็น RESERVED ที่คลังเดิม พร้อมบรรทัด/ref การจองเดิม
        public static async Task<UnitOpResult> ReviveAsync(
            JewelryContext context,
            TbtStockPiece piece,
            string soldRef,
            ReservationKey key,
            decimal qty,
            StockUnitSyncInfo info)
        {
            var count = StockUnitSync.ToUnits(qty);
            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);

            var sold = units.Where(u => u.Status == StockUnitSync.Sold && u.SoldRef == soldRef).ToList();
            var chosen = sold
                .OrderBy(u => key.Matches(u) ? 0 : (u.ReservedLineId == null ? 1 : 2))
                .ThenByDescending(u => u.UnitNo)
                .Take(count)
                .ToList();

            foreach (var unit in chosen)
            {
                unit.Status = StockUnitSync.Reserved;
                unit.SoldRef = null;
                unit.SoldDate = null;
                unit.ReservedRefType ??= key.RefType;
                unit.ReservedRef ??= key.Ref;
                unit.ReservedLineId ??= key.LineId;
                Touch(unit, info);

                result.AddDelta(unit.LocationCode, 1, 1);
                result.AddLine(null, unit.LocationCode, unit);
            }

            var missing = count - chosen.Count;
            if (missing > 0)
            {
                var location = piece.LocationCode;
                var nextNo = units.Count == 0 ? 1 : units.Max(u => u.UnitNo) + 1;

                for (var i = 0; i < missing; i++)
                {
                    var unit = new TbtStockUnit
                    {
                        StockNumber = piece.StockNumber,
                        ProductCode = piece.ProductCode,
                        UnitNo = nextNo++,
                        LocationCode = location,
                        Status = StockUnitSync.Reserved,
                        ReservedRefType = key.RefType,
                        ReservedRef = key.Ref,
                        ReservedLineId = key.LineId,
                        ReceiptNumber = piece.ReceiptNumber,
                        ReceiptDate = piece.ReceiptDate,
                        CreateDate = info.Now,
                        CreateBy = info.By
                    };
                    context.TbtStockUnit.Add(unit);
                    units.Add(unit);

                    result.AddDelta(location, 1, 1);
                    result.AddLine(null, location, unit);
                }
            }

            var revived = chosen.Count + Math.Max(0, missing);
            piece.Qty += revived;
            piece.QtyReserved += revived;
            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** Add units (receipt / top-up) ***

        public static async Task<UnitOpResult> AddUnitsAsync(
            JewelryContext context,
            TbtStockPiece piece,
            int count,
            string location,
            StockUnitSyncInfo info)
        {
            if (count <= 0)
            {
                throw new HandleException("จำนวนที่เพิ่มต้องมากกว่า 0");
            }

            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);

            var nextNo = units.Count == 0 ? 1 : units.Max(u => u.UnitNo) + 1;
            var receiptNumber = info.ReceiptNumber;
            var receiptDate = info.ReceiptDate;
            if (receiptNumber == null && receiptDate == null)
            {
                receiptNumber = piece.ReceiptNumber;
                receiptDate = piece.ReceiptDate;
            }

            for (var i = 0; i < count; i++)
            {
                var unit = new TbtStockUnit
                {
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    UnitNo = nextNo++,
                    LocationCode = location,
                    Status = StockUnitSync.InStock,
                    ReceiptNumber = receiptNumber,
                    ReceiptDate = receiptDate,
                    CreateDate = info.Now,
                    CreateBy = info.By
                };
                context.TbtStockUnit.Add(unit);
                units.Add(unit);

                result.AddDelta(location, 1, 0);
                result.AddLine(null, location, unit);
            }

            piece.Qty += count;
            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** Move ***

        // ย้ายชิ้นไปคลังปลายทาง — เลือกชิ้นด้วย unitIds, หรือ (fromLocation + qty), หรือทั้งล็อต (ไม่ระบุทั้งคู่)
        // ชิ้น SOLD ย้ายไม่ได้ · ชิ้นที่อยู่คลังปลายทางอยู่แล้วข้ามเงียบๆ · ชิ้นจอง (RESERVED) ย้ายได้ พร้อมการจองไปด้วย
        public static async Task<UnitOpResult> MoveAsync(
            JewelryContext context,
            TbtStockPiece piece,
            IReadOnlyList<long>? unitIds,
            string? fromLocation,
            int? qty,
            string targetLocation,
            StockUnitSyncInfo info)
        {
            var units = await LoadLotAsync(context, piece, info);
            var result = new UnitOpResult(piece);
            var target = NormalizeLocation(targetLocation)!;
            var from = NormalizeLocation(fromLocation);
            List<TbtStockUnit> selected;

            if (unitIds != null && unitIds.Count > 0)
            {
                if (unitIds.Distinct().Count() != unitIds.Count)
                {
                    throw new HandleException($"ระบุชิ้นซ้ำกันในสินค้า {piece.StockNumber}");
                }

                selected = new List<TbtStockUnit>();
                foreach (var id in unitIds)
                {
                    var unit = units.FirstOrDefault(u => u.Id == id);
                    if (unit == null)
                    {
                        throw new HandleException($"ชิ้นที่เลือกไม่อยู่ในสินค้า {piece.StockNumber}");
                    }

                    if (unit.Status == StockUnitSync.Sold)
                    {
                        throw new HandleException($"ไม่สามารถย้ายสินค้า {piece.StockNumber} ชิ้นที่ {unit.UnitNo} ได้ เนื่องจากขายแล้ว");
                    }

                    selected.Add(unit);
                }
            }
            else if (from != null && qty.HasValue)
            {
                if (qty.Value <= 0)
                {
                    throw new HandleException($"จำนวนที่ย้ายของสินค้า {piece.StockNumber} ต้องมากกว่า 0");
                }

                var atFrom = units
                    .Where(u => u.Status != StockUnitSync.Sold && SameLocation(u.LocationCode, from))
                    .ToList();

                if (atFrom.Count < qty.Value)
                {
                    throw new HandleException($"สินค้า {piece.StockNumber} มีชิ้นที่คลัง {from} {atFrom.Count} ชิ้น แต่ขอย้าย {qty.Value} ชิ้น");
                }

                var tagged = await TaggedIdsAsync(context, piece);
                selected = atFrom
                    .OrderBy(u => u.Status == StockUnitSync.Reserved ? 1 : 0)
                    .ThenBy(u => u.Id != 0 && tagged.Contains(u.Id) ? 1 : 0)
                    .ThenByDescending(u => u.UnitNo)
                    .Take(qty.Value)
                    .ToList();
            }
            else
            {
                selected = units.Where(u => u.Status != StockUnitSync.Sold).ToList();
                if (selected.Count == 0)
                {
                    throw new HandleException($"ไม่สามารถย้ายคลังสินค้า {piece.StockNumber} ได้ เนื่องจากไม่มีชิ้นคงเหลือ (สถานะ {piece.Status ?? "ไม่ทราบสถานะ"})");
                }
            }

            foreach (var unit in selected)
            {
                if (SameLocation(unit.LocationCode, target))
                {
                    continue;
                }

                var source = unit.LocationCode;
                var reserved = unit.Status == StockUnitSync.Reserved ? 1 : 0;

                unit.LocationCode = target;
                Touch(unit, info);

                result.AddDelta(source, -1, -reserved);
                result.AddDelta(target, 1, reserved);
                result.AddLine(source, target, unit);
            }

            if (result.Units.Count == 0)
            {
                return result;
            }

            Finish(context, result, units, info);
            return result;
        }

        #endregion

        #region *** helpers ***

        private static void Finish(JewelryContext context, UnitOpResult result, List<TbtStockUnit> allUnits, StockUnitSyncInfo info)
        {
            var piece = result.Piece;

            StockPieceQtyHelper.RecalcStatus(piece);
            StockUnitSync.RecalcPrimaryLocation(piece, allUnits);
            piece.UpdateDate = info.Now;
            piece.UpdateBy = info.By;
            context.TbtStockPiece.Update(piece);

            // unit ที่เพิ่งสร้างอยู่ในสถานะ Added แล้ว (Id ชั่วคราว ≠ 0) ห้าม Update ซ้ำ — Update เฉพาะ unit เดิมจาก DB
            var existing = result.Units
                .Distinct()
                .Where(u => context.Entry(u).State != EntityState.Added && context.Entry(u).State != EntityState.Detached)
                .ToList();
            if (existing.Count > 0)
            {
                context.TbtStockUnit.UpdateRange(existing);
            }
        }

        // ใบจองเก่าที่ยังไม่มีชิ้นผูก: RESERVED ที่ไม่มี ref/line — คลังหลักก่อน แล้วชิ้นไม่มีแท็ก แล้ว unit_no สูงสุด
        private static async Task<List<TbtStockUnit>> PickLegacyReservedAsync(JewelryContext context, TbtStockPiece piece, List<TbtStockUnit> units, int count)
        {
            if (count <= 0) return new List<TbtStockUnit>();

            var tagged = await TaggedIdsAsync(context, piece);

            return units
                .Where(u => u.Status == StockUnitSync.Reserved && u.ReservedRefType == null && u.ReservedLineId == null)
                .OrderBy(u => u.LocationCode == piece.LocationCode ? 0 : 1)
                .ThenBy(u => u.Id != 0 && tagged.Contains(u.Id) ? 1 : 0)
                .ThenByDescending(u => u.UnitNo)
                .Take(count)
                .ToList();
        }

        private static async Task<HashSet<long>> TaggedIdsAsync(JewelryContext context, TbtStockPiece piece)
        {
            var tags = await StockUnitSync.LoadActiveTagsAsync(context, piece.StockNumber, piece.ProductCode);
            return tags.Where(t => t.UnitId.HasValue).Select(t => t.UnitId!.Value).ToHashSet();
        }

        // ชิ้นที่ยังไม่มีแท็กก่อน แล้ว unit_no สูงสุด
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

        private static string? NormalizeLocation(string? code)
        {
            var trimmed = code?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToUpperInvariant();
        }

        private static bool SameLocation(string? a, string? b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
