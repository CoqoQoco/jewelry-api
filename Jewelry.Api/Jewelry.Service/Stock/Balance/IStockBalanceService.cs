using System.Linq;

namespace Jewelry.Service.Stock.Balance
{
    public interface IStockBalanceService
    {
        IQueryable<object> List();
    }
}
