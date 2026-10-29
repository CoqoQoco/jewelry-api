using jewelry.Model.Stock.Movement.Move;
using System.Linq;
using System.Threading.Tasks;
using SearchModel = jewelry.Model.Stock.Movement.Search;

namespace Jewelry.Service.Stock.Movement
{
    public interface IStockMovementService
    {
        IQueryable<object> List();
        Task<Response> MoveLocation(Request req);
        Task<jewelry.Model.Stock.Movement.MoveUnits.Response> MoveUnits(jewelry.Model.Stock.Movement.MoveUnits.Request req);
        IQueryable<SearchModel.Response> Search(SearchModel.Request request);
    }
}
