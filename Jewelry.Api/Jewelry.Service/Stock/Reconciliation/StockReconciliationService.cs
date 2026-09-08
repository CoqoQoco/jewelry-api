using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Model.Stock.Reconciliation;
using Microsoft.EntityFrameworkCore;
using System.Transactions;


namespace Jewelry.Service.Stock.Reconciliation
{
    public class StockReconciliationService : IStockReconciliationService
    {
        private readonly JewelryContext _jewelryContext;

        public StockReconciliationService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<ReconciliationReport> CheckDriftAsync(CancellationToken ct)
        {
            var report = new ReconciliationReport
            {
                CheckedAt = DateTime.UtcNow
            };

            report.NewPieceCount = await _jewelryContext.TbtStockPiece
                .AsNoTracking().CountAsync(ct);

            var pieceGroups = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .GroupBy(p => new { p.SkuCode, p.LocationCode })
                .Select(g => new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    // IN_STOCK/RESERVED เท่านั้น กัน piece SOLD เก่า (ก่อนขึ้น qty) ที่ยังเหลือ qty=1 ค้างจาก default ของ migration
                    ExpectedOnHand   = g.Sum(p => (p.Status == "IN_STOCK" || p.Status == "RESERVED") ? p.Qty : 0),
                    // ไม่กรอง status เพราะล็อตที่จองบางส่วนยังเป็น IN_STOCK (qtyReserved < qty) แต่ต้องนับ qtyReserved ด้วย
                    ExpectedReserved = g.Sum(p => p.QtyReserved)
                })
                .ToListAsync(ct);

            var balances = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Select(b => new { b.SkuCode, b.LocationCode, b.QtyOnHand, b.QtyReserved })
                .ToListAsync(ct);

            var balanceMap = balances
                .ToDictionary(b => (b.SkuCode, b.LocationCode));

            var pieceKeys = new HashSet<(string, string)>(
                pieceGroups.Select(p => (p.SkuCode, p.LocationCode)));

            var onHandMismatches = new List<BalanceMismatch>();
            var reservedMismatches = new List<BalanceMismatch>();

            foreach (var pg in pieceGroups)
            {
                balanceMap.TryGetValue((pg.SkuCode, pg.LocationCode), out var bal);
                var actualOnHand   = bal == null ? 0m : bal.QtyOnHand;
                var actualReserved = bal == null ? 0m : bal.QtyReserved;

                if (actualOnHand != pg.ExpectedOnHand)
                {
                    onHandMismatches.Add(new BalanceMismatch
                    {
                        SkuCode = pg.SkuCode,
                        LocationCode = pg.LocationCode,
                        BalanceQty = actualOnHand,
                        PieceCount = pg.ExpectedOnHand
                    });
                }
                if (actualReserved != pg.ExpectedReserved)
                {
                    reservedMismatches.Add(new BalanceMismatch
                    {
                        SkuCode = pg.SkuCode,
                        LocationCode = pg.LocationCode,
                        BalanceQty = actualReserved,
                        PieceCount = pg.ExpectedReserved
                    });
                }
            }

            foreach (var b in balances)
            {
                if (pieceKeys.Contains((b.SkuCode, b.LocationCode))) continue;
                if (b.QtyOnHand != 0)
                {
                    onHandMismatches.Add(new BalanceMismatch
                    {
                        SkuCode = b.SkuCode,
                        LocationCode = b.LocationCode,
                        BalanceQty = b.QtyOnHand,
                        PieceCount = 0
                    });
                }
                if (b.QtyReserved != 0)
                {
                    reservedMismatches.Add(new BalanceMismatch
                    {
                        SkuCode = b.SkuCode,
                        LocationCode = b.LocationCode,
                        BalanceQty = b.QtyReserved,
                        PieceCount = 0
                    });
                }
            }

            report.OnHandMismatches = onHandMismatches.Take(100).ToList();
            report.BalanceOnHandMismatchCount = onHandMismatches.Count;
            report.ReservedMismatches = reservedMismatches.Take(100).ToList();
            report.BalanceReservedMismatchCount = reservedMismatches.Count;

            return report;
        }

        public async Task<RebuildBalanceResult> RebuildBalanceFromPiecesAsync(CancellationToken ct)
        {
            const int batchSize = 500;

            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // โหลด balance ทั้งหมด (tracked) ไว้ upsert — ห้ามลบทิ้งเพราะ ~22,000 แถวสร้างโดยผู้ใช้จริงตอนรับของ
            var existingBalances = await _jewelryContext.TbtStockBalance.ToListAsync(ct);
            var balanceMap = existingBalances.ToDictionary(b => (b.SkuCode, b.LocationCode));

            var aggregated = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .GroupBy(p => new { p.SkuCode, p.LocationCode })
                .Select(g => new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    QtyOnHand = g.Sum(p => (p.Status == "IN_STOCK" || p.Status == "RESERVED") ? p.Qty : 0),
                    QtyReserved = g.Sum(p => p.QtyReserved)
                })
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            var result = new RebuildBalanceResult();
            var pendingUpdates = new List<TbtStockBalance>();
            var pendingInserts = new List<TbtStockBalance>();
            var touchedKeys = new HashSet<(string SkuCode, string LocationCode)>();

            async Task FlushAsync()
            {
                if (pendingUpdates.Count > 0)
                {
                    _jewelryContext.TbtStockBalance.UpdateRange(pendingUpdates);
                    pendingUpdates.Clear();
                }
                if (pendingInserts.Count > 0)
                {
                    _jewelryContext.TbtStockBalance.AddRange(pendingInserts);
                    pendingInserts.Clear();
                }
                await _jewelryContext.SaveChangesAsync(ct);
            }

            foreach (var a in aggregated)
            {
                var key = (a.SkuCode, a.LocationCode);
                touchedKeys.Add(key);

                if (balanceMap.TryGetValue(key, out var existing))
                {
                    if (existing.QtyOnHand != a.QtyOnHand || existing.QtyReserved != a.QtyReserved)
                    {
                        existing.QtyOnHand = a.QtyOnHand;
                        existing.QtyReserved = a.QtyReserved;
                        existing.LastMovementAt = now;
                        existing.UpdateDate = now;
                        existing.UpdateBy = "RECONCILE";
                        pendingUpdates.Add(existing);
                        result.UpdatedCount++;
                    }
                    else
                    {
                        result.UnchangedCount++;
                    }
                }
                else
                {
                    pendingInserts.Add(new TbtStockBalance
                    {
                        SkuCode = a.SkuCode,
                        LocationCode = a.LocationCode,
                        QtyOnHand = a.QtyOnHand,
                        QtyReserved = a.QtyReserved,
                        LastMovementAt = now,
                        CreateDate = now,
                        CreateBy = "RECONCILE"
                    });
                    result.InsertedCount++;
                }

                if (pendingUpdates.Count + pendingInserts.Count >= batchSize)
                {
                    await FlushAsync();
                }
            }

            // balance ที่ไม่มี piece เหลือเลย (เช่น piece ถูกย้ายคลังออกไปหมด) — เคลียร์ยอดเป็น 0 แทนการลบแถว
            foreach (var kv in balanceMap)
            {
                if (touchedKeys.Contains(kv.Key)) continue;

                var existing = kv.Value;
                if (existing.QtyOnHand != 0 || existing.QtyReserved != 0)
                {
                    existing.QtyOnHand = 0;
                    existing.QtyReserved = 0;
                    existing.LastMovementAt = now;
                    existing.UpdateDate = now;
                    existing.UpdateBy = "RECONCILE";
                    pendingUpdates.Add(existing);
                    result.ZeroedCount++;
                }
                else
                {
                    result.UnchangedCount++;
                }

                if (pendingUpdates.Count + pendingInserts.Count >= batchSize)
                {
                    await FlushAsync();
                }
            }

            await FlushAsync();

            scope.Complete();

            return result;
        }
    }
}
