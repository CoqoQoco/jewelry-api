using System.Threading.Tasks;
using jewelry.Model.Material.Insight;
using Min = jewelry.Model.Material.Insight.Min;
using Overview = jewelry.Model.Material.Insight.Overview;
using Policy = jewelry.Model.Material.Insight.Policy;

namespace Jewelry.Service.Material.Insight
{
    public interface IMaterialInsightService
    {
        Task<Overview.Response> Overview(Overview.Request request);
        Task<Policy.Response> GetPolicy(Policy.GetRequest request);
        Task<Policy.Response> SavePolicy(Policy.SaveRequest request);
        Task<Min.Response> SaveMin(Min.SaveRequest request);
        Task<Min.Response> DeleteMin(Min.DeleteRequest request);
    }
}
