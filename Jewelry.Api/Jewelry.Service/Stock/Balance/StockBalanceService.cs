using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Jewelry.Service.Stock.Balance
{
    public class StockBalanceService : IStockBalanceService
    {
        private readonly JewelryContext _jewelryContext;

        public StockBalanceService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }

        public async Task<List<jewelry.Model.Stock.Balance.ByStockNumbers.Response>> ListByStockNumbersAsync(List<string> stockNumbers, CancellationToken ct)
        {
            if (stockNumbers == null || !stockNumbers.Any())
                return new List<jewelry.Model.Stock.Balance.ByStockNumbers.Response>();

            var result = await (from p in _jewelryContext.TbtStockPiece.AsNoTracking()
                                where stockNumbers.Contains(p.StockNumber)
                                join b in _jewelryContext.TbtStockBalance.AsNoTracking()
                                    on new { p.SkuCode, p.LocationCode } equals new { b.SkuCode, b.LocationCode }
                                    into bg
                                from b in bg.DefaultIfEmpty()
                                select new jewelry.Model.Stock.Balance.ByStockNumbers.Response
                                {
                                    StockNumber = p.StockNumber,
                                    SkuCode = p.SkuCode,
                                    LocationCode = p.LocationCode,
                                    PieceStatus = p.Status,
                                    QtyOnHand = b != null ? b.QtyOnHand : 0,
                                    QtyReserved = b != null ? b.QtyReserved : 0,
                                    QtyAvailable = b != null ? (b.QtyAvailable ?? 0) : 0
                                }).ToListAsync(ct);
            return result;
        }

        public async Task<jewelry.Model.Stock.Balance.Summary.Response> GetSummary(jewelry.Model.Stock.Balance.Summary.Request request)
        {
            var query = _jewelryContext.TbtStockBalance.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(request.LocationCode))
            {
                query = query.Where(x => x.LocationCode == request.LocationCode);
            }

            var overall = await query
                .GroupBy(x => 1)
                .Select(g => new jewelry.Model.Stock.Balance.Summary.OverallItem
                {
                    SkuCount = g.Select(x => x.SkuCode).Distinct().Count(),
                    LocationCount = g.Select(x => x.LocationCode).Distinct().Count(),
                    TotalOnHand = g.Sum(x => x.QtyOnHand),
                    TotalReserved = g.Sum(x => x.QtyReserved),
                    TotalAvailable = g.Sum(x => x.QtyAvailable ?? 0)
                })
                .FirstOrDefaultAsync() ?? new jewelry.Model.Stock.Balance.Summary.OverallItem();

            var byLocation = await query
                .GroupBy(x => new { x.LocationCode, x.LocationCodeNavigation.NameTh })
                .Select(g => new jewelry.Model.Stock.Balance.Summary.ByLocationItem
                {
                    LocationCode = g.Key.LocationCode,
                    LocationName = g.Key.NameTh,
                    SkuCount = g.Select(x => x.SkuCode).Distinct().Count(),
                    TotalOnHand = g.Sum(x => x.QtyOnHand),
                    TotalReserved = g.Sum(x => x.QtyReserved),
                    TotalAvailable = g.Sum(x => x.QtyAvailable ?? 0)
                })
                .OrderByDescending(x => x.TotalOnHand)
                .ToListAsync();

            return new jewelry.Model.Stock.Balance.Summary.Response
            {
                Overall = overall,
                ByLocation = byLocation
            };
        }
    }
}
