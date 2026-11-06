using jewelry.Model.ReleaseLog;
using System.Threading.Tasks;

namespace Jewelry.Service.ReleaseLog;

public interface IReleaseLogService
{
    Task<long> Create(CreateReleaseLogRequest request);
    Task<LatestReleaseLogResponse> Latest();
}
