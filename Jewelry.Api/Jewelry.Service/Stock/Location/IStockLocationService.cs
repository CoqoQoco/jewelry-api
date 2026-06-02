using jewelry.Model.Stock.Location;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Location
{
    public interface IStockLocationService
    {
        IQueryable<object> List();
        Task<string> Create(CreateStockLocationRequest req);
        Task<string> Update(UpdateStockLocationRequest req);
        Task Delete(string code);
    }
}
