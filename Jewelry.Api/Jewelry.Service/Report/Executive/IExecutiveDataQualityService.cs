using System.Threading.Tasks;
using DataQuality = jewelry.Model.Report.Executive.DataQuality;

namespace Jewelry.Service.Report.Executive
{
    public interface IExecutiveDataQualityService
    {
        Task<DataQuality.Response> DataQuality(DataQuality.Request request);
    }
}
