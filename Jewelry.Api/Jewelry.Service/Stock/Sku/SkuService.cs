using Jewelry.Data.Context;
using System.Linq;

namespace Jewelry.Service.Stock.Sku
{
    public class SkuService : ISkuService
    {
        private readonly JewelryContext _jewelryContext;

        public SkuService(JewelryContext jewelryContext)
        {
            _jewelryContext = jewelryContext;
        }

        public IQueryable<object> List()
        {
            return Enumerable.Empty<object>().AsQueryable();
        }
    }
}
