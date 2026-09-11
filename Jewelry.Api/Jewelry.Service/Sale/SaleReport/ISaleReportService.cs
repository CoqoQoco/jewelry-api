using System.Collections.Generic;
using System.Threading.Tasks;
using ByChannel = jewelry.Model.Sale.SaleReport.ByChannel;

namespace Jewelry.Service.Sale.SaleReport
{
    public interface ISaleReportService
    {
        Task<jewelry.Model.Sale.SaleReport.PipelineSummary.Response> PipelineSummary(jewelry.Model.Sale.SaleReport.PipelineSummary.Request request);
        Task<List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>> CustomerProductionStatus(jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Request request);
        Task<ByChannel.Response> ByChannel(ByChannel.Request request);
    }
}
