using System.Linq;

namespace Jewelry.Service.Stock.Location
{
    public interface IStockLocationService
    {
        IQueryable<object> List();
    }
}
