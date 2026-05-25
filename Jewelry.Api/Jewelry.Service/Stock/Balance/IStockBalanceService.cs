using System.Linq;

namespace Jewelry.Service.Stock.Balance
{
    public interface IStockBalanceService
    {
        IQueryable<object> List();
        Task<List<jewelry.Model.Stock.Balance.ByStockNumbers.Response>> ListByStockNumbersAsync(List<string> stockNumbers, CancellationToken ct);
    }
}
