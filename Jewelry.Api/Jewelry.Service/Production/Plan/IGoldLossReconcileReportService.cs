using System.Threading.Tasks;

namespace Jewelry.Service.Production.Plan
{
    public interface IGoldLossReconcileReportService
    {
        Task<jewelry.Model.Production.Plan.GoldLossReconcileReport.SearchResponse> GetGoldLossReconcileReport(jewelry.Model.Production.Plan.GoldLossReconcileReport.SearchRequest request);
    }
}
