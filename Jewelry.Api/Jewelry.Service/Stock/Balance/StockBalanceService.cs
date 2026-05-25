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
    }
}
