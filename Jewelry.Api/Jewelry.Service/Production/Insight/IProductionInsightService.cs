using System.Collections.Generic;
using System.Threading.Tasks;
using Kendo.DynamicLinqCore;
using Wip = jewelry.Model.Production.Insight.Wip;
using WipTrend = jewelry.Model.Production.Insight.WipTrend;
using DueRiskPlans = jewelry.Model.Production.Insight.DueRiskPlans;
using StageLeadTime = jewelry.Model.Production.Insight.StageLeadTime;
using AbnormalDwellPlans = jewelry.Model.Production.Insight.AbnormalDwellPlans;
using StageStandards = jewelry.Model.Production.Insight.StageStandards;
using SaveStageStandards = jewelry.Model.Production.Insight.SaveStageStandards;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;
using Delivery = jewelry.Model.Production.Insight.Delivery;
using DeliveryAtRiskPlans = jewelry.Model.Production.Insight.DeliveryAtRiskPlans;
using DeliveryLatePlans = jewelry.Model.Production.Insight.DeliveryLatePlans;
using StuckAfterCostCardPlans = jewelry.Model.Production.Insight.StuckAfterCostCardPlans;
using DeliveryTarget = jewelry.Model.Production.Insight.DeliveryTarget;
using SaveDeliveryTarget = jewelry.Model.Production.Insight.SaveDeliveryTarget;

namespace Jewelry.Service.Production.Insight
{
    public interface IProductionInsightService
    {
        Task<Wip.Response> Wip(Wip.Request request);
        Task<WipTrend.Response> WipTrend(WipTrend.Request request);
        Task<DataSourceResult> StalePlans(StalePlans.Request request);
        Task<DataSourceResult> DueRiskPlans(DueRiskPlans.Request request);
        Task<StageLeadTime.Response> StageLeadTime(StageLeadTime.Request request);
        Task<DataSourceResult> AbnormalDwellPlans(AbnormalDwellPlans.Request request);
        Task<List<StageStandards.Item>> GetStageStandards();
        Task<List<StageStandards.Item>> GetStageStandardHistory(string deptKey);
        Task SaveStageStandards(SaveStageStandards.Request request);

        Task<Delivery.Response> Delivery(Delivery.Request request);
        Task<DataSourceResult> DeliveryAtRiskPlans(DeliveryAtRiskPlans.Request request);
        Task<DataSourceResult> DeliveryLatePlans(DeliveryLatePlans.Request request);
        Task<DataSourceResult> StuckAfterCostCardPlans(StuckAfterCostCardPlans.Request request);
        Task<DeliveryTarget.Item?> GetDeliveryTarget();
        Task<List<DeliveryTarget.Item>> GetDeliveryTargetHistory();
        Task SaveDeliveryTarget(SaveDeliveryTarget.Request request);
    }
}
