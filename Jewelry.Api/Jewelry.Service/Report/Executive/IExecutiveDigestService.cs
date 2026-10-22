using System.Threading.Tasks;
using Digest = jewelry.Model.Report.Executive.Digest;

namespace Jewelry.Service.Report.Executive
{
    public interface IExecutiveDigestService
    {
        Task<Digest.Response> Digest(Digest.Request request);
    }
}
