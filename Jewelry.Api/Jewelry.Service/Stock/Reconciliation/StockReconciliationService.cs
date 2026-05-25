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
                .AsNoTracking()
                .CountAsync(ct);

            report.NewPieceCount = await _jewelryContext.TbtStockPiece
                .AsNoTracking()
                .CountAsync(ct);

            var onHandMismatches = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Join(
                    _jewelryContext.TbtStockPiece
                        .AsNoTracking()
                        .Where(p => p.Status == "IN_STOCK")
                        .GroupBy(p => new { p.SkuCode, p.LocationCode })
                        .Select(g => new { g.Key.SkuCode, g.Key.LocationCode, PieceCount = g.Count() }),
                    b => new { b.SkuCode, b.LocationCode },
                    p => new { p.SkuCode, p.LocationCode },
                    (b, p) => new { Balance = b, Piece = p }
                )
                .Where(x => (int)x.Balance.QtyOnHand != x.Piece.PieceCount)
                .Select(x => new BalanceMismatch
                {
                    SkuCode = x.Balance.SkuCode,
                    LocationCode = x.Balance.LocationCode,
                    BalanceQty = x.Balance.QtyOnHand,
                    PieceCount = x.Piece.PieceCount
                })
                .Take(100)
                .ToListAsync(ct);

            // Also capture balances that have no matching pieces
            var balancesWithNoPieces = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Where(b => b.QtyOnHand != 0)
                .Where(b => !_jewelryContext.TbtStockPiece
                    .Any(p => p.SkuCode == b.SkuCode && p.LocationCode == b.LocationCode && p.Status == "IN_STOCK"))
                .Select(b => new BalanceMismatch
                {
                    SkuCode = b.SkuCode,
                    LocationCode = b.LocationCode,
                    BalanceQty = b.QtyOnHand,
                    PieceCount = 0
                })
                .Take(100)
                .ToListAsync(ct);

            report.OnHandMismatches = onHandMismatches
                .Concat(balancesWithNoPieces)
                .Take(100)
                .ToList();
            report.BalanceOnHandMismatchCount = report.OnHandMismatches.Count;

            var reservedMismatches = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Join(
                    _jewelryContext.TbtStockPiece
                        .AsNoTracking()
                        .Where(p => p.Status == "RESERVED")
                        .GroupBy(p => new { p.SkuCode, p.LocationCode })
                        .Select(g => new { g.Key.SkuCode, g.Key.LocationCode, PieceCount = g.Count() }),
                    b => new { b.SkuCode, b.LocationCode },
                    p => new { p.SkuCode, p.LocationCode },
                    (b, p) => new { Balance = b, Piece = p }
                )
                .Where(x => (int)x.Balance.QtyReserved != x.Piece.PieceCount)
                .Select(x => new BalanceMismatch
                {
                    SkuCode = x.Balance.SkuCode,
                    LocationCode = x.Balance.LocationCode,
                    BalanceQty = x.Balance.QtyReserved,
                    PieceCount = x.Piece.PieceCount
                })
                .Take(100)
                .ToListAsync(ct);

            var balancesWithNoReservedPieces = await _jewelryContext.TbtStockBalance
                .AsNoTracking()
                .Where(b => b.QtyReserved != 0)
                .Where(b => !_jewelryContext.TbtStockPiece
                    .Any(p => p.SkuCode == b.SkuCode && p.LocationCode == b.LocationCode && p.Status == "RESERVED"))
                .Select(b => new BalanceMismatch
                {
                    SkuCode = b.SkuCode,
                    LocationCode = b.LocationCode,
                    BalanceQty = b.QtyReserved,
                    PieceCount = 0
                })
                .Take(100)
                .ToListAsync(ct);

            report.ReservedMismatches = reservedMismatches
                .Concat(balancesWithNoReservedPieces)
                .Take(100)
                .ToList();
            report.BalanceReservedMismatchCount = report.ReservedMismatches.Count;

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
