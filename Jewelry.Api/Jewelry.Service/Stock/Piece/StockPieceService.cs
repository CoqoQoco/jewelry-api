using Jewelry.Data.Context;
using System.Linq;

namespace Jewelry.Service.Stock.Piece
{
    public class StockPieceService : IStockPieceService
    {
        private readonly JewelryContext _jewelryContext;

        public StockPieceService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }
    }
}
