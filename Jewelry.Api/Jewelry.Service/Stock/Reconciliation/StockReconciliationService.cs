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

            // TK202610290003: ล็อตเดียวอยู่ได้หลายคลัง — ยอดที่ควรเป็นของ (SKU x คลัง) นับจาก unit ที่ไม่ SOLD ตามคลังของแต่ละชิ้น
            // (SKU มาจาก piece) ไม่ใช่จาก piece.LocationCode
            var pieceGroups = await (
                from u in _jewelryContext.TbtStockUnit.AsNoTracking()
                where u.Status != "SOLD"
                join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                    on new { u.StockNumber, u.ProductCode } equals new { p.StockNumber, p.ProductCode }
                group u by new { p.SkuCode, u.LocationCode } into g
                select new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    ExpectedOnHand = (decimal)g.Count(),
                    ExpectedReserved = (decimal)g.Count(x => x.Status == "RESERVED")
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

            // จำนวน unit ต้องตรงกับ qty / qty_reserved ของ piece (ถ้าไม่ตรง ยอดรายคลังข้างบนเชื่อถือไม่ได้)
            // set-based: ล็อต + unit รวมกลุ่ม แล้วเทียบใน memory (ไม่ใช้ correlated subquery ต่อล็อต)
            var lots = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Select(p => new { p.StockNumber, p.ProductCode, p.Qty, p.QtyReserved })
                .ToListAsync(ct);

            var unitCounts = await _jewelryContext.TbtStockUnit
                .AsNoTracking()
                .Where(u => u.Status != "SOLD")
                .GroupBy(u => new { u.StockNumber, u.ProductCode, u.Status })
                .Select(g => new { g.Key.StockNumber, g.Key.ProductCode, g.Key.Status, Count = g.Count() })
                .ToListAsync(ct);

            var unitQtyByLot = new Dictionary<(string, string), int>();
            var unitReservedByLot = new Dictionary<(string, string), int>();
            foreach (var u in unitCounts)
            {
                var key = (u.StockNumber, u.ProductCode);
                unitQtyByLot[key] = unitQtyByLot.GetValueOrDefault(key) + u.Count;
                if (u.Status == "RESERVED")
                {
                    unitReservedByLot[key] = unitReservedByLot.GetValueOrDefault(key) + u.Count;
                }
            }

            var unitMismatches = lots
                .Select(p =>
                {
                    var key = (p.StockNumber, p.ProductCode);
                    return new UnitCountMismatch
                    {
                        StockNumber = p.StockNumber,
                        ProductCode = p.ProductCode,
                        PieceQty = p.Qty,
                        UnitQty = unitQtyByLot.GetValueOrDefault(key),
                        PieceReserved = p.QtyReserved,
                        UnitReserved = unitReservedByLot.GetValueOrDefault(key)
                    };
                })
                .Where(x => Math.Floor(x.PieceQty) != x.UnitQty
                    || Math.Floor(x.PieceReserved <= x.PieceQty ? x.PieceReserved : x.PieceQty) != x.UnitReserved)
                .ToList();

            report.UnitCountMismatchCount = unitMismatches.Count;
            report.UnitCountMismatches = unitMismatches.Take(100).ToList();

            report.OnHandMismatches = onHandMismatches.Take(100).ToList();
            report.BalanceOnHandMismatchCount = onHandMismatches.Count;
            report.ReservedMismatches = reservedMismatches.Take(100).ToList();
            report.BalanceReservedMismatchCount = reservedMismatches.Count;

            return report;
        }

        private static readonly string[] LedgerMovementTypes = { "RECEIPT", "RETURN", "TRANSFORM_IN", "SALE", "TRANSFORM_OUT" };

        // ledger = Σ ± qty ของ movement ต่อล็อต (กติกา StockLedger.QtySign: รวม TRANSFORM_IN/OUT ของการแปลงสินค้า)
        // ยอดจองที่ควรเป็น = บรรทัด SO ที่ยังไม่ออกบิล (ไม่นับ placeholder — รายการรอของไม่เคยจองของจริง) + ต้นทางของใบแปลงที่ยังไม่เสร็จ (จองด้วย ref CONVERT)
        // set-based: 4 query แบน (ล็อต, movement รวมกลุ่ม, SO, ใบแปลง) แล้วเทียบใน memory — ไม่ใช้ correlated subquery ต่อล็อต
        public async Task<PieceLedgerReport> CheckPieceLedgerAsync(CancellationToken ct)
        {
            var lots = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Select(p => new PieceLedgerRow
                {
                    StockNumber = p.StockNumber,
                    ProductCode = p.ProductCode,
                    Status = p.Status,
                    Qty = p.Qty,
                    QtyReserved = p.QtyReserved
                })
                .ToListAsync(ct);

            var movementSums = await _jewelryContext.TbtStockMovement
                .AsNoTracking()
                .Where(m => m.StockNumber != null && m.ProductCode != null && LedgerMovementTypes.Contains(m.MovementType))
                .GroupBy(m => new { m.StockNumber, m.ProductCode, m.MovementType })
                .Select(g => new { g.Key.StockNumber, g.Key.ProductCode, g.Key.MovementType, Qty = g.Sum(x => x.Qty) })
                .ToListAsync(ct);

            var ledgerByLot = new Dictionary<(string, string), decimal>();
            foreach (var m in movementSums)
            {
                var key = (m.StockNumber!, m.ProductCode!);
                ledgerByLot[key] = ledgerByLot.GetValueOrDefault(key) + StockLedger.QtySign(m.MovementType) * m.Qty;
            }

            var openSoLines = await _jewelryContext.TbtSaleOrderProduct
                .AsNoTracking()
                .Where(sop => sop.Invoice == null && !sop.IsPlaceholder)
                .GroupBy(sop => sop.StockNumber)
                .Select(g => new { StockNumber = g.Key, Qty = g.Sum(x => x.Qty) })
                .ToListAsync(ct);

            var reservedByStock = new Dictionary<string, decimal>();
            foreach (var l in openSoLines)
            {
                reservedByStock[l.StockNumber] = reservedByStock.GetValueOrDefault(l.StockNumber) + l.Qty;
            }

            var openConvertSources = await (
                from i in _jewelryContext.TbtStockConvertItem.AsNoTracking()
                join h in _jewelryContext.TbtStockConvertHeader.AsNoTracking() on i.HeaderRunning equals h.Running
                where i.Role == "SOURCE" && h.Status == 0
                select new { i.StockNumber, i.Qty })
                .ToListAsync(ct);

            foreach (var c in openConvertSources)
            {
                reservedByStock[c.StockNumber] = reservedByStock.GetValueOrDefault(c.StockNumber) + c.Qty;
            }

            var items = new List<PieceLedgerIssue>();
            foreach (var x in lots)
            {
                x.LedgerQty = ledgerByLot.GetValueOrDefault((x.StockNumber, x.ProductCode));
                x.ConfirmedUninvoicedQty = reservedByStock.GetValueOrDefault(x.StockNumber);

                var issues = new List<string>();
                if (x.Qty != x.LedgerQty) issues.Add("LEDGER_MISMATCH");
                if (x.QtyReserved != x.ConfirmedUninvoicedQty) issues.Add("RESERVED_MISMATCH");
                if (x.QtyReserved > x.Qty) issues.Add("RESERVED_OVER_QTY");
                if (x.Qty < 0) issues.Add("NEGATIVE_QTY");
                if (issues.Count == 0) continue;

                items.Add(new PieceLedgerIssue
                {
                    StockNumber = x.StockNumber,
                    ProductCode = x.ProductCode,
                    Status = x.Status,
                    Qty = x.Qty,
                    QtyReserved = x.QtyReserved,
                    LedgerQty = x.LedgerQty,
                    ConfirmedUninvoicedQty = x.ConfirmedUninvoicedQty,
                    Issues = issues
                });
            }

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

            // ยอดรายคลังนับจาก unit ที่ไม่ SOLD ตามคลังของแต่ละชิ้น (ล็อตเดียวอยู่ได้หลายคลัง) — SKU มาจาก piece
            var aggregated = await (
                from u in _jewelryContext.TbtStockUnit.AsNoTracking()
                where u.Status != "SOLD"
                join p in _jewelryContext.TbtStockPiece.AsNoTracking()
                    on new { u.StockNumber, u.ProductCode } equals new { p.StockNumber, p.ProductCode }
                group u by new { p.SkuCode, u.LocationCode } into g
                select new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    QtyOnHand = (decimal)g.Count(),
                    QtyReserved = (decimal)g.Count(x => x.Status == "RESERVED")
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
