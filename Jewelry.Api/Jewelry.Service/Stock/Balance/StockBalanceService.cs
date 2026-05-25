using Jewelry.Data.Context;
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
    }
}
