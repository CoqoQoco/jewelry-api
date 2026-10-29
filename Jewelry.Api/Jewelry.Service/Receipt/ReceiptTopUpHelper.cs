using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Jewelry.Service.Receipt.Production;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt
{
    internal sealed class TopUpRow
    {
        public string StockNumber { get; set; } = null!;
        public bool IsSilver { get; set; }
        public string? Location { get; set; }
        public decimal Qty { get; set; }
        public string ReceiptNumber { get; set; } = null!;

        // คลังที่รับเข้าจริงของแถวนี้ (resolve จาก Location ใน LoadAndValidateAsync) — ล็อตเดียวหลายคลัง: เติมเข้าคลังนี้ได้แม้ต่างจากคลังหลักของล็อต
        public string? ResolvedLocation { get; set; }
    }

    // เติมจำนวนเข้า lot เดิมจากหน้ารับสินค้า (ทอง/เงิน) — แตะเฉพาะ qty/status ของ piece + unit + balance + movement เท่านั้น
    // ชิ้นที่เติมเข้าคลังตามที่รับจริง (ไม่ต้องตรงกับคลังหลักของล็อต)
    internal static class ReceiptTopUpHelper
    {
        public static string? CleanStockNumber(string? value)
        {
            var t = value?.Trim();
            return string.IsNullOrEmpty(t) ? null : t;
        }

        public static async Task<Dictionary<string, TbtStockPiece>> LoadAndValidateAsync(
            JewelryContext context,
            IReadOnlyCollection<TopUpRow> rows,
            Dictionary<string, string> locationCache,
            string operatorBy,
            bool track)
        {
            var result = new Dictionary<string, TbtStockPiece>();
            if (rows.Count == 0) return result;

            var numbers = rows.Select(r => r.StockNumber).Distinct().ToList();

            var query = context.TbtStockPiece.Where(x => numbers.Contains(x.StockNumber));
            if (!track)
            {
                query = query.AsNoTracking();
            }
            var pieces = await query.ToListAsync();

            foreach (var number in numbers)
            {
                var found = pieces.Where(x => x.StockNumber == number).ToList();
                if (found.Count == 0)
                {
                    throw new HandleException($"ไม่พบล็อต {number}");
                }
                if (found.Count > 1)
                {
                    throw new HandleException($"พบล็อต {number} มากกว่า 1 รายการ ไม่สามารถเติมจำนวนได้");
                }
                result[number] = found[0];
            }

            var skuCodes = result.Values.Select(x => x.SkuCode).Distinct().ToList();
            var productionTypeBySku = (await context.TbtSku
                .AsNoTracking()
                .Where(s => skuCodes.Contains(s.SkuCode))
                .Select(s => new { s.SkuCode, s.ProductionType })
                .ToListAsync())
                .ToDictionary(x => x.SkuCode, x => x.ProductionType);

            foreach (var row in rows)
            {
                var piece = result[row.StockNumber];

                row.ResolvedLocation = await ReceiptProductionServiceExtention.ResolveLocationCodeAsync(context, row.Location, operatorBy, locationCache);

                productionTypeBySku.TryGetValue(piece.SkuCode, out var lotProductionType);
                var lotIsSilver = MetalClassifier.IsSilver(lotProductionType);
                if (row.IsSilver && !lotIsSilver)
                {
                    throw new HandleException($"ล็อต {piece.StockNumber} ไม่ใช่สินค้าเงิน ไม่สามารถเติมจำนวนด้วยสินค้าเงินได้");
                }
                if (!row.IsSilver && lotIsSilver)
                {
                    throw new HandleException($"ล็อต {piece.StockNumber} เป็นสินค้าเงิน ไม่สามารถเติมจำนวนด้วยสินค้าทองได้");
                }
            }

            return result;
        }

        public static async Task ApplyAsync(
            JewelryContext context,
            IReadOnlyCollection<TopUpRow> rows,
            Dictionary<string, TbtStockPiece> pieces,
            Dictionary<string, TbtStockBalance> balanceCache,
            List<TbtStockBalance> upsertBalances,
            List<TbtStockMovement> newMovements,
            string operatorBy)
        {
            var now = DateTime.UtcNow;
            var guarded = new HashSet<string>();

            foreach (var row in rows)
            {
                var piece = pieces[row.StockNumber];
                var n = row.Qty;

                // ล็อตที่ขายหมดต้องมี qty = 0 — ถ้าข้อมูลเพี้ยน (SOLD แต่ qty != 0) ให้ถือเป็น 0 ก่อนบวก
                if (guarded.Add(piece.StockNumber) && piece.Status == "SOLD" && piece.Qty != 0)
                {
                    piece.Qty = 0;
                }

                var location = row.ResolvedLocation ?? piece.LocationCode;

                var add = await StockUnitOps.AddUnitsAsync(context, piece, StockUnitSync.ToUnits(n), location, new StockUnitSyncInfo
                {
                    By = operatorBy,
                    Now = now,
                    ReceiptNumber = row.ReceiptNumber,
                    ReceiptDate = now
                });

                var balanceKey = $"{piece.SkuCode}|{location}";
                if (!balanceCache.TryGetValue(balanceKey, out var balance))
                {
                    balance = await context.TbtStockBalance
                        .FirstOrDefaultAsync(x => x.SkuCode == piece.SkuCode && x.LocationCode == location);
                    if (balance == null)
                    {
                        balance = new TbtStockBalance
                        {
                            SkuCode = piece.SkuCode,
                            LocationCode = location,
                            QtyOnHand = 0,
                            QtyReserved = 0,
                            CreateBy = operatorBy,
                            CreateDate = now
                        };
                        upsertBalances.Add(balance);
                    }
                    balanceCache[balanceKey] = balance;
                }
                balance.QtyOnHand += n;
                balance.LastMovementAt = now;

                var movement = ReceiptProductionServiceExtention.MapNewReceiptMovement(
                    piece.SkuCode, piece.StockNumber, piece.ProductCode, location, row.ReceiptNumber, operatorBy, qty: n);
                newMovements.Add(movement);

                foreach (var unit in add.Units)
                {
                    context.TbtStockMovementUnit.Add(new TbtStockMovementUnit { Movement = movement, Unit = unit });
                }
            }
        }
    }
}
