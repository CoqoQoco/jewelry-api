using jewelry.Model.Report.Executive.Flow;
using System.Threading.Tasks;

namespace Jewelry.Service.Report.Executive
{
    public interface IExecutiveFlowService
    {
        Task<FlowBoardResponse> FlowBoard(FlowBoardRequest request);
        Task<FlowZoneResponse> FlowZone(FlowZoneRequest request);
    }
}
