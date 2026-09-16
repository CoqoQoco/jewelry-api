using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Model.Stock.Reconciliation;
using Jewelry.Service.Stock;
using Microsoft.EntityFrameworkCore;


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

        public async Task<PieceLedgerReport> CheckPieceLedgerAsync(CancellationToken ct)
        {
            // เงื่อนไขความผิดปกติทั้งหมดเช็คในนี้ (correlated subquery ต่อ piece) ให้ postgres กรองให้ ไม่โหลด piece ทั้งหมด (~30k แถว) มาวนใน memory
            var mismatchQuery = _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Where(p =>
                    p.Qty != _jewelryContext.TbtStockMovement
                        .Where(m => m.StockNumber == p.StockNumber && m.ProductCode == p.ProductCode)
                        .Sum(m => m.MovementType == "RECEIPT" || m.MovementType == "RETURN"
                            ? m.Qty
                            : (m.MovementType == "SALE" ? -m.Qty : 0m))
                    || p.QtyReserved != _jewelryContext.TbtSaleOrderProduct
                        .Where(sop => sop.StockNumber == p.StockNumber && sop.Invoice == null)
                        .Sum(sop => sop.Qty)
                    || p.QtyReserved > p.Qty
                    || p.Qty < 0);

            var mismatches = await mismatchQuery
                .Select(p => new PieceLedgerRow
                {
                    StockNumber = p.StockNumber,
                    ProductCode = p.ProductCode,
                    Status = p.Status,
                    Qty = p.Qty,
                    QtyReserved = p.QtyReserved,
                    LedgerQty = _jewelryContext.TbtStockMovement
                        .Where(m => m.StockNumber == p.StockNumber && m.ProductCode == p.ProductCode)
                        .Sum(m => m.MovementType == "RECEIPT" || m.MovementType == "RETURN"
                            ? m.Qty
                            : (m.MovementType == "SALE" ? -m.Qty : 0m)),
                    ConfirmedUninvoicedQty = _jewelryContext.TbtSaleOrderProduct
                        .Where(sop => sop.StockNumber == p.StockNumber && sop.Invoice == null)
                        .Sum(sop => sop.Qty)
                })
                .ToListAsync(ct);

            var items = mismatches.Select(x =>
            {
                var issues = new List<string>();
                if (x.Qty != x.LedgerQty) issues.Add("LEDGER_MISMATCH");
                if (x.QtyReserved != x.ConfirmedUninvoicedQty) issues.Add("RESERVED_MISMATCH");
                if (x.QtyReserved > x.Qty) issues.Add("RESERVED_OVER_QTY");
                if (x.Qty < 0) issues.Add("NEGATIVE_QTY");

                return new PieceLedgerIssue
                {
                    StockNumber = x.StockNumber,
                    ProductCode = x.ProductCode,
                    Status = x.Status,
                    Qty = x.Qty,
                    QtyReserved = x.QtyReserved,
                    LedgerQty = x.LedgerQty,
                    ConfirmedUninvoicedQty = x.ConfirmedUninvoicedQty,
                    Issues = issues
                };
            }).ToList();

            return new PieceLedgerReport
            {
                CheckedAt = DateTime.UtcNow,
                IssueCount = items.Count,
                Items = items.Take(100).ToList()
            };
        }

        private class PieceLedgerRow
        {
            public string StockNumber { get; set; } = null!;
            public string ProductCode { get; set; } = null!;
            public string? Status { get; set; }
            public decimal Qty { get; set; }
            public decimal QtyReserved { get; set; }
            public decimal LedgerQty { get; set; }
            public decimal ConfirmedUninvoicedQty { get; set; }
        }

        public async Task<RebuildBalanceResult> RebuildBalanceFromPiecesAsync(CancellationToken ct)
        {
            const int batchSize = 500;

            // อ่านรายชื่อ key ของ balance ทั้งหมดแบบ no-tracking ไว้แค่รู้ว่ามี key ไหนอยู่บ้าง (ใช้หา key ที่ไม่มี piece เหลือแล้วในขั้นถัดไป)
            // เป็นการอ่าน snapshot เฉยๆ ไม่ล็อก — งาน repair ที่ admin กดเอง ไม่ใช่ path ขายที่วิ่งตลอดเวลา
            var allBalanceKeys = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Select(b => new { b.SkuCode, b.LocationCode })
                .ToListAsync(ct);
            var allBalanceKeySet = allBalanceKeys.Select(b => (b.SkuCode, b.LocationCode)).ToHashSet();

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
            var touchedKeys = new HashSet<(string SkuCode, string LocationCode)>();

            // ตารางนี้มีเป็นหมื่นแถว ล็อกทั้งตารางในทีเดียวไม่สมเหตุสมผล จึงล็อกทีละ batch ตาม SKU ของ batch นั้น แล้ว commit ทันที (ปล่อยล็อก) ก่อนไป batch ถัดไป
            // ผลคือระหว่าง batch มีหน้าต่างเล็กๆ ที่แถวยังไม่ถูกล็อก ถ้ามีการขาย/รับของพร้อมกันพอดีในช่วงนั้นอาจเห็นค่ากลางคัน แต่ยอมรับได้เพราะเป็นงาน repair ที่ admin กดเอง ไม่ใช่ path realtime
            foreach (var batch in aggregated.Chunk(batchSize))
            {
                foreach (var a in batch)
                {
                    touchedKeys.Add((a.SkuCode, a.LocationCode));
                }

                var skuCodes = batch.Select(a => a.SkuCode).Distinct().ToList();

                var ownsTransaction = _jewelryContext.Database.CurrentTransaction == null;
                var transaction = ownsTransaction
                    ? await _jewelryContext.Database.BeginTransactionAsync(ct)
                    : null;

                try
                {
                    await _jewelryContext.LockStockBalancesAsync(skuCodes);

                    var freshBalances = await _jewelryContext.TbtStockBalance
                        .Where(b => skuCodes.Contains(b.SkuCode))
                        .ToListAsync(ct);
                    var freshMap = freshBalances.ToDictionary(b => (b.SkuCode, b.LocationCode));

                    var pendingUpdates = new List<TbtStockBalance>();
                    var pendingInserts = new List<TbtStockBalance>();

                    foreach (var a in batch)
                    {
                        var key = (a.SkuCode, a.LocationCode);

                        if (freshMap.TryGetValue(key, out var existing))
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
                    }

                    if (pendingUpdates.Count > 0)
                    {
                        _jewelryContext.TbtStockBalance.UpdateRange(pendingUpdates);
                    }
                    if (pendingInserts.Count > 0)
                    {
                        _jewelryContext.TbtStockBalance.AddRange(pendingInserts);
                    }

                    await _jewelryContext.SaveChangesAsync(ct);

                    if (ownsTransaction)
                    {
                        await transaction!.CommitAsync(ct);
                    }
                }
                catch
                {
                    if (ownsTransaction)
                    {
                        await transaction!.RollbackAsync(ct);
                    }
                    throw;
                }
                finally
                {
                    if (ownsTransaction)
                    {
                        await transaction!.DisposeAsync();
                    }
                }
            }

            // balance ที่ไม่มี piece เหลือเลย (เช่น piece ถูกย้ายคลังออกไปหมด) — เคลียร์ยอดเป็น 0 แทนการลบแถว ล็อกทีละ batch เหมือนกัน
            var untouchedKeys = allBalanceKeySet.Where(k => !touchedKeys.Contains(k)).ToList();

            foreach (var batch in untouchedKeys.Chunk(batchSize))
            {
                var skuCodes = batch.Select(k => k.SkuCode).Distinct().ToList();

                var ownsTransaction = _jewelryContext.Database.CurrentTransaction == null;
                var transaction = ownsTransaction
                    ? await _jewelryContext.Database.BeginTransactionAsync(ct)
                    : null;

                try
                {
                    await _jewelryContext.LockStockBalancesAsync(skuCodes);

                    var freshBalances = await _jewelryContext.TbtStockBalance
                        .Where(b => skuCodes.Contains(b.SkuCode))
                        .ToListAsync(ct);
                    var freshMap = freshBalances.ToDictionary(b => (b.SkuCode, b.LocationCode));

                    var pendingUpdates = new List<TbtStockBalance>();

                    foreach (var key in batch)
                    {
                        if (!freshMap.TryGetValue(key, out var existing)) continue;

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
                    }

                    if (pendingUpdates.Count > 0)
                    {
                        _jewelryContext.TbtStockBalance.UpdateRange(pendingUpdates);
                    }

                    await _jewelryContext.SaveChangesAsync(ct);

                    if (ownsTransaction)
                    {
                        await transaction!.CommitAsync(ct);
                    }
                }
                catch
                {
                    if (ownsTransaction)
                    {
                        await transaction!.RollbackAsync(ct);
                    }
                    throw;
                }
                finally
                {
                    if (ownsTransaction)
                    {
                        await transaction!.DisposeAsync();
                    }
                }
            }

            return result;
        }
    }
}
