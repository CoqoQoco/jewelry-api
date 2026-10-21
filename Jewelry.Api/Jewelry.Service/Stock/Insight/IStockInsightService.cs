using System.Threading.Tasks;
using Overview = jewelry.Model.Stock.Insight.Overview;

namespace Jewelry.Service.Stock.Insight
{
    public interface IStockInsightService
    {
        Task<Overview.Response> Overview(Overview.Request request);
    }
}
