using System.Threading.Tasks;
using Kendo.DynamicLinqCore;
using Summary = jewelry.Model.Report.Executive.Summary;
using ProductionWip = jewelry.Model.Report.Executive.ProductionWip;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;
using Receivables = jewelry.Model.Report.Executive.Receivables;
using SalesOrdersWithoutInvoice = jewelry.Model.Report.Executive.SalesOrdersWithoutInvoice;
using StockHealth = jewelry.Model.Report.Executive.StockHealth;

namespace Jewelry.Service.Report.Executive
{
    public interface IExecutiveReportService
    {
        Task<Summary.Response> Summary(Summary.Request request);
        Task<ProductionWip.Response> ProductionWip(ProductionWip.Request request);
        Task<DataSourceResult> StalePlans(StalePlans.Request request);
        Task<DataSourceResult> Receivables(Receivables.Request request);
        Task<DataSourceResult> SalesOrdersWithoutInvoice(SalesOrdersWithoutInvoice.Request request);
        Task<StockHealth.Response> StockHealth(StockHealth.Request request);
    }
}
