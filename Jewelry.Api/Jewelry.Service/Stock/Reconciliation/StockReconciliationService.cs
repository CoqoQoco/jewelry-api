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

            report.LegacyStockCount = await _jewelryContext.TbtStockProduct
                .AsNoTracking().CountAsync(ct);

            report.NewPieceCount = await _jewelryContext.TbtStockPiece
                .AsNoTracking().CountAsync(ct);

            var pieceGroups = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .GroupBy(p => new { p.SkuCode, p.LocationCode })
                .Select(g => new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    ExpectedOnHand   = g.Count(p => p.Status == "IN_STOCK" || p.Status == "RESERVED"),
                    ExpectedReserved = g.Count(p => p.Status == "RESERVED")
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

                if ((int)actualOnHand != pg.ExpectedOnHand)
                {
                    onHandMismatches.Add(new BalanceMismatch
                    {
                        SkuCode = pg.SkuCode,
                        LocationCode = pg.LocationCode,
                        BalanceQty = actualOnHand,
                        PieceCount = pg.ExpectedOnHand
                    });
                }
                if ((int)actualReserved != pg.ExpectedReserved)
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

            report.StatusMismatches = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .Join(
                    _jewelryContext.TbtStockProduct.AsNoTracking(),
                    piece => piece.StockNumber,
                    legacy => legacy.StockNumber,
                    (piece, legacy) => new { piece, legacy }
                )
                .Where(x =>
                    (x.piece.Status == "SOLD" && x.legacy.QtySale == 0) ||
                    (x.piece.Status != "SOLD" && x.legacy.QtySale > 0 && x.piece.Status == "IN_STOCK"))
                .Select(x => new StatusMismatch
                {
                    StockNumber = x.piece.StockNumber,
                    PieceStatus = x.piece.Status,
                    LegacyQtySale = x.legacy.QtySale
                })
                .Take(100)
                .ToListAsync(ct);

            report.StatusMismatchCount = report.StatusMismatches.Count;

            report.LegacyQtySaleMismatches = await _jewelryContext.TbtStockProduct
                .AsNoTracking()
                .Join(
                    _jewelryContext.TbtStockPiece.AsNoTracking(),
                    legacy => legacy.StockNumber,
                    piece => piece.StockNumber,
                    (legacy, piece) => new { legacy, piece }
                )
                .Where(x =>
                    (x.legacy.QtySale > 0 && x.piece.Status != "RESERVED" && x.piece.Status != "SOLD") ||
                    (x.legacy.QtySale == 0 && x.piece.Status == "RESERVED"))
                .Select(x => new QtySaleMismatch
                {
                    StockNumber = x.legacy.StockNumber,
                    LegacyQtySale = x.legacy.QtySale,
                    PieceStatus = x.piece.Status
                })
                .Take(100)
                .ToListAsync(ct);

            report.LegacyQtySaleMismatchCount = report.LegacyQtySaleMismatches.Count;

            return report;
        }

        public async Task<int> RebuildBalanceFromPiecesAsync(CancellationToken ct)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var toDelete = await _jewelryContext.TbtStockBalance
                .Where(b => b.CreateBy == "BACKFILL" || b.CreateBy == "RECONCILE")
                .ToListAsync(ct);

            _jewelryContext.TbtStockBalance.RemoveRange(toDelete);
            await _jewelryContext.SaveChangesAsync(ct);

            var aggregated = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .GroupBy(p => new { p.SkuCode, p.LocationCode })
                .Select(g => new
                {
                    g.Key.SkuCode,
                    g.Key.LocationCode,
                    QtyOnHand = g.Count(p => p.Status == "IN_STOCK" || p.Status == "RESERVED"),
                    QtyReserved = g.Count(p => p.Status == "RESERVED")
                })
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            var newBalances = aggregated.Select(a => new TbtStockBalance
            {
                SkuCode = a.SkuCode,
                LocationCode = a.LocationCode,
                QtyOnHand = a.QtyOnHand,
                QtyReserved = a.QtyReserved,
                LastMovementAt = now,
                CreateDate = now,
                CreateBy = "RECONCILE"
            }).ToList();

            _jewelryContext.TbtStockBalance.AddRange(newBalances);
            await _jewelryContext.SaveChangesAsync(ct);

            scope.Complete();

            return newBalances.Count;
        }
    }
}
