using System.Linq;

namespace Jewelry.Service.Stock.Sku
{
    public interface ISkuService
    {
        IQueryable<object> List();
    }
}
