using System.Threading.Tasks;
using Kendo.DynamicLinqCore;
using Wip = jewelry.Model.Production.Insight.Wip;
using DueRiskPlans = jewelry.Model.Production.Insight.DueRiskPlans;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionInsightService
    {
        Task<Wip.Response> Wip(Wip.Request request);
        Task<DataSourceResult> StalePlans(StalePlans.Request request);
        Task<DataSourceResult> DueRiskPlans(DueRiskPlans.Request request);
    }
}
