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
    }

    // เติมจำนวนเข้า lot เดิมจากหน้ารับสินค้า (ทอง/เงิน) — แตะเฉพาะ qty/status ของ piece + balance + movement เท่านั้น
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

                var locationCode = await ReceiptProductionServiceExtention.ResolveLocationCodeAsync(context, row.Location, operatorBy, locationCache);
                if (!string.Equals(locationCode, piece.LocationCode, StringComparison.Ordinal))
                {
                    throw new HandleException($"ล็อต {piece.StockNumber} อยู่คลัง {piece.LocationCode} ไม่ตรงกับคลังที่รับ ({locationCode}) กรุณาสร้างเลขใหม่");
                }

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

                piece.Qty += n;
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateBy = operatorBy;
                piece.UpdateDate = now;

                var balanceKey = $"{piece.SkuCode}|{piece.LocationCode}";
                if (!balanceCache.TryGetValue(balanceKey, out var balance))
                {
                    balance = await context.TbtStockBalance
                        .FirstOrDefaultAsync(x => x.SkuCode == piece.SkuCode && x.LocationCode == piece.LocationCode);
                    if (balance == null)
                    {
                        balance = new TbtStockBalance
                        {
                            SkuCode = piece.SkuCode,
                            LocationCode = piece.LocationCode,
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

                newMovements.Add(ReceiptProductionServiceExtention.MapNewReceiptMovement(
                    piece.SkuCode, piece.StockNumber, piece.ProductCode, piece.LocationCode, row.ReceiptNumber, operatorBy, qty: n));
            }
        }
    }
}
