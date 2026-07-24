using System.Threading.Tasks;

namespace Jewelry.Service.Sale.SaleReport
{
    public interface ISaleReportService
    {
        Task<jewelry.Model.Sale.SaleReport.PipelineSummary.Response> PipelineSummary(jewelry.Model.Sale.SaleReport.PipelineSummary.Request request);
    }
}
