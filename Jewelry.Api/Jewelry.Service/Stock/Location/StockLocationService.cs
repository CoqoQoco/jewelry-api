using Jewelry.Data.Context;
using System.Linq;

namespace Jewelry.Service.Stock.Location
{
    public class StockLocationService : IStockLocationService
    {
        private readonly JewelryContext _jewelryContext;

        public StockLocationService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }
    }
}
