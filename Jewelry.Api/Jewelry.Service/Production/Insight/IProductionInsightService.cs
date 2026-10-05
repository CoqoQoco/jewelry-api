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
using Gold = jewelry.Model.Production.Insight.Gold;
using GoldOverSlips = jewelry.Model.Production.Insight.GoldOverSlips;
using GoldUncoveredJobs = jewelry.Model.Production.Insight.GoldUncoveredJobs;
using GoldLossTarget = jewelry.Model.Production.Insight.GoldLossTarget;
using SaveGoldLossTargets = jewelry.Model.Production.Insight.SaveGoldLossTargets;
using Capacity = jewelry.Model.Production.Insight.Capacity;
using CostCardPendingPlans = jewelry.Model.Production.Insight.CostCardPendingPlans;
using GoldByStage = jewelry.Model.Production.Insight.GoldByStage;
using GoldStageOutlierJobs = jewelry.Model.Production.Insight.GoldStageOutlierJobs;
using GoldStagePendingReturn = jewelry.Model.Production.Insight.GoldStagePendingReturn;
using Workers = jewelry.Model.Production.Insight.Workers;
using WorkerMonthly = jewelry.Model.Production.Insight.WorkerMonthly;
using UnpaidPieceJobs = jewelry.Model.Production.Insight.UnpaidPieceJobs;
using Materials = jewelry.Model.Production.Insight.Materials;
using MaterialWaitingPlans = jewelry.Model.Production.Insight.MaterialWaitingPlans;
using MaterialGemDemand = jewelry.Model.Production.Insight.MaterialGemDemand;
using MaterialGemLowCover = jewelry.Model.Production.Insight.MaterialGemLowCover;

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

        Task<Gold.Response> Gold(Gold.Request request);
        Task<DataSourceResult> GoldOverSlips(GoldOverSlips.Request request);
        Task<DataSourceResult> GoldUncoveredJobs(GoldUncoveredJobs.Request request);
        Task<List<GoldLossTarget.Item>> GetGoldLossTargets();
        Task<List<GoldLossTarget.Item>> GetGoldLossTargetHistory(string scope, int workerType, string metal);
        Task SaveGoldLossTargets(SaveGoldLossTargets.Request request);

        Task<Capacity.Response> Capacity(Capacity.Request request);
        Task<DataSourceResult> CostCardPendingPlans(CostCardPendingPlans.Request request);

        Task<GoldByStage.Response> GoldByStage(GoldByStage.Request request);
        Task<DataSourceResult> GoldStageOutlierJobs(GoldStageOutlierJobs.Request request);
        Task<DataSourceResult> GoldStagePendingReturn(GoldStagePendingReturn.Request request);

        Task<Workers.Response> Workers(Workers.Request request);
        Task<WorkerMonthly.Response> WorkerMonthly(WorkerMonthly.Request request);
        Task<DataSourceResult> UnpaidPieceJobs(UnpaidPieceJobs.Request request);

        Task<Materials.Response> Materials(Materials.Request request);
        Task<DataSourceResult> MaterialWaitingPlans(MaterialWaitingPlans.Request request);
        Task<DataSourceResult> MaterialGemDemand(MaterialGemDemand.Request request);
        Task<DataSourceResult> MaterialGemLowCover(MaterialGemLowCover.Request request);
    }
}
