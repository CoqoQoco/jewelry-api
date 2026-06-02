using jewelry.Model.Stock.Movement.Move;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.Movement
{
    public interface IStockMovementService
    {
        IQueryable<object> List();
        Task<Response> MoveLocation(Request req);
    }
}
