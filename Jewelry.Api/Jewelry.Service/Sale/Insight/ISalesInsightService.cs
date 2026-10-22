using System.Threading.Tasks;
using Overview = jewelry.Model.Sale.Insight.Overview;

namespace Jewelry.Service.Sale.Insight
{
    public interface ISalesInsightService
    {
        Task<Overview.Response> Overview(Overview.Request request);
    }
}
