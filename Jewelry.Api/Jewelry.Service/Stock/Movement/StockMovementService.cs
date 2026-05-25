using Jewelry.Data.Context;
using System.Linq;

namespace Jewelry.Service.Stock.Movement
{
    public class StockMovementService : IStockMovementService
    {
        private readonly JewelryContext _jewelryContext;

        public StockMovementService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }
    }
}
