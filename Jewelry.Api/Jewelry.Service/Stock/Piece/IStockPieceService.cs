using System.Linq;

namespace Jewelry.Service.Stock.Piece
{
    public interface IStockPieceService
    {
        IQueryable<object> List();
    }
}
