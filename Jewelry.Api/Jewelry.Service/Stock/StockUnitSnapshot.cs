using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock
{
    // ล็อต (tbt_stock_piece) ที่ยังมีของ + คุณสมบัติ SKU ที่รายงานสต็อกต้องใช้ — flat projection ไม่มี navigation เหลือ
    public class StockSnapshotLot
    {
        public string StockNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public string SkuCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public string? Status { get; set; }
        public decimal Qty { get; set; }
        public decimal QtyReserved { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? ReceiptType { get; set; }
        public decimal? ProductCost { get; set; }
        public DateTime? ProductionDate { get; set; }
        public string? StockNumberOrigin { get; set; }
        public string? ProductNumber { get; set; }
        public string? ProductNameTh { get; set; }
        public string? ProductType { get; set; }
        public string? ProductTypeName { get; set; }
        public string? Mold { get; set; }
        public string? MoldDesign { get; set; }
        public string? ProductionType { get; set; }
        public string? ProductionTypeSize { get; set; }
        public string? ImagePath { get; set; }
        public string? ImageName { get; set; }
    }

    // unit (ชิ้นจริง) ที่ไม่ SOLD รวมกลุ่มตาม ล็อต x คลัง x สถานะ x ใบรับ
    public class StockUnitGroup
    {
        public string StockNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public string LocationCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? ReceiptNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public int Pcs { get; set; }
    }

    // ชิ้นของล็อตหนึ่งที่อยู่คลังเดียวกัน สถานะเดียวกัน และอายุเริ่มนับวันเดียวกัน — หน่วยนับของรายงานสต็อกทุกตัว (Pcs = จำนวนชิ้น ไม่ใช่จำนวนแถว)
    public class StockUnitBucket
    {
        public StockSnapshotLot Lot { get; set; } = null!;
        public string Location { get; set; } = null!;
        // IN_STOCK (พร้อมขาย) | RESERVED (จองแล้ว)
        public string Status { get; set; } = null!;
        public int Pcs { get; set; }
        // วันที่เริ่มนับอายุ: ชิ้นที่มาจากการเติมล็อต = วันที่รับรอบเติม · นอกนั้น = production_date ของล็อต
        public DateTime? AgeDate { get; set; }
    }

    // รายงานสต็อกทุกตัวนับจาก snapshot นี้: 3 query แบน (ล็อต, unit รวมกลุ่ม, ใบเติมเงินระบบเก่า) แล้วรวมใน memory
    // ห้ามใช้ GroupBy + First() ใน LINQ (กลายเป็น correlated subquery ต่อกลุ่ม)
    public static class StockUnitSnapshot
    {
        public const string InStock = "IN_STOCK";
        public const string Reserved = "RESERVED";

        public static async Task<List<StockUnitBucket>> LoadAsync(JewelryContext context, CancellationToken ct = default)
        {
            var lots = await context.TbtStockPiece
                .AsNoTracking()
                .Where(x => x.Status != "SOLD" && x.Qty > 0)
                .Select(x => new StockSnapshotLot
                {
                    StockNumber = x.StockNumber,
                    ProductCode = x.ProductCode,
                    SkuCode = x.SkuCode,
                    LocationCode = x.LocationCode,
                    Status = x.Status,
                    Qty = x.Qty,
                    QtyReserved = x.QtyReserved,
                    ReceiptNumber = x.ReceiptNumber,
                    ReceiptType = x.ReceiptType,
                    ProductCost = x.ProductCost,
                    ProductionDate = x.ProductionDate,
                    StockNumberOrigin = x.StockNumberOrigin,
                    ProductNumber = x.SkuCodeNavigation.ProductNumber,
                    ProductNameTh = x.SkuCodeNavigation.ProductNameTh,
                    ProductType = x.SkuCodeNavigation.ProductType,
                    ProductTypeName = x.SkuCodeNavigation.ProductTypeName,
                    Mold = x.SkuCodeNavigation.Mold,
                    MoldDesign = x.SkuCodeNavigation.MoldDesign,
                    ProductionType = x.SkuCodeNavigation.ProductionType,
                    ProductionTypeSize = x.SkuCodeNavigation.ProductionTypeSize,
                    ImagePath = x.SkuCodeNavigation.ImagePath,
                    ImageName = x.SkuCodeNavigation.ImageName
                })
                .ToListAsync(ct);

            var groups = await context.TbtStockUnit
                .AsNoTracking()
                .Where(u => u.Status != "SOLD")
                .GroupBy(u => new { u.StockNumber, u.ProductCode, u.LocationCode, u.Status, u.ReceiptNumber, u.ReceiptDate })
                .Select(g => new StockUnitGroup
                {
                    StockNumber = g.Key.StockNumber,
                    ProductCode = g.Key.ProductCode,
                    LocationCode = g.Key.LocationCode,
                    Status = g.Key.Status,
                    ReceiptNumber = g.Key.ReceiptNumber,
                    ReceiptDate = g.Key.ReceiptDate,
                    Pcs = g.Count()
                })
                .ToListAsync(ct);

            var topUpRefs = await context.TbtStockMovement
                .AsNoTracking()
                .Where(m => m.RefDocType == StockLedger.SilverTopUpRefDocType && m.RefDocNo != null)
                .Select(m => m.RefDocNo!)
                .Distinct()
                .ToListAsync(ct);

            return Build(lots, groups, new HashSet<string>(topUpRefs, StringComparer.Ordinal));
        }

        public static List<StockUnitBucket> Build(
            IReadOnlyList<StockSnapshotLot> lots,
            IReadOnlyList<StockUnitGroup> groups,
            ISet<string> legacySilverTopUpRefs)
        {
            var byLot = groups.ToLookup(g => (g.StockNumber, g.ProductCode));
            var buckets = new List<StockUnitBucket>(groups.Count + 16);

            foreach (var lot in lots)
            {
                var parts = byLot[(lot.StockNumber, lot.ProductCode)].ToList();
                if (parts.Count == 0)
                {
                    AddSynthesized(buckets, lot);
                    continue;
                }

                foreach (var g in parts)
                {
                    if (g.Pcs <= 0) continue;

                    buckets.Add(new StockUnitBucket
                    {
                        Lot = lot,
                        Location = g.LocationCode,
                        Status = g.Status == Reserved ? Reserved : InStock,
                        Pcs = g.Pcs,
                        AgeDate = ResolveAgeDate(lot, g, legacySilverTopUpRefs)
                    });
                }
            }

            return buckets;
        }

        // ชิ้นที่ใบรับไม่ตรงกับใบรับของล็อต = เติมล็อตทีหลัง → อายุนับจากวันรับรอบเติม
        // ยกเว้นใบเติมที่ย้ายมาจากระบบเก่า (TRANSFER_SILVER_TOPUP) ที่วันรับคือวันนำเข้าระบบ ไม่ใช่วันที่ผลิต
        private static DateTime? ResolveAgeDate(StockSnapshotLot lot, StockUnitGroup g, ISet<string> legacySilverTopUpRefs)
        {
            var isTopUp = g.ReceiptNumber != null
                && g.ReceiptNumber != lot.ReceiptNumber
                && !legacySilverTopUpRefs.Contains(g.ReceiptNumber);

            return isTopUp
                ? g.ReceiptDate ?? lot.ProductionDate
                : lot.ProductionDate ?? g.ReceiptDate;
        }

        // ล็อตที่ยังไม่มี unit (ตาข่ายนิรภัย ควรเป็น 0) นับจาก qty / qty_reserved ที่คลังหลักของล็อต
        private static void AddSynthesized(List<StockUnitBucket> buckets, StockSnapshotLot lot)
        {
            var total = StockUnitSync.ToUnits(lot.Qty);
            var reserved = Math.Min(StockUnitSync.ToUnits(lot.QtyReserved), total);

            if (total - reserved > 0)
            {
                buckets.Add(new StockUnitBucket { Lot = lot, Location = lot.LocationCode, Status = InStock, Pcs = total - reserved, AgeDate = lot.ProductionDate });
            }
            if (reserved > 0)
            {
                buckets.Add(new StockUnitBucket { Lot = lot, Location = lot.LocationCode, Status = Reserved, Pcs = reserved, AgeDate = lot.ProductionDate });
            }
        }
    }
}
