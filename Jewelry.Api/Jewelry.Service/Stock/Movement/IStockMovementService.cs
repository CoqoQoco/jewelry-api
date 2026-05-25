using System.Linq;

namespace Jewelry.Service.Stock.Movement
{
    public interface IStockMovementService
    {
        IQueryable<object> List();
    }
}
