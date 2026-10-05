using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Production.Shared;
using Jewelry.Service.Report.Executive;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ProductionPlanStatusConst = jewelry.Model.Constant.ProductionPlanStatus;
using Wip = jewelry.Model.Production.Insight.Wip;
using WipTrend = jewelry.Model.Production.Insight.WipTrend;
using DueRiskPlans = jewelry.Model.Production.Insight.DueRiskPlans;
using StageLeadTime = jewelry.Model.Production.Insight.StageLeadTime;
using AbnormalDwellPlans = jewelry.Model.Production.Insight.AbnormalDwellPlans;
using StageStandards = jewelry.Model.Production.Insight.StageStandards;
using SaveStageStandards = jewelry.Model.Production.Insight.SaveStageStandards;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;
using ExecutiveProductionWip = jewelry.Model.Report.Executive.ProductionWip;
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
    public class ProductionInsightService : BaseService, IProductionInsightService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IProductionPlanWipHelper _wipHelper;
        private readonly IExecutiveReportService _executiveReportService;
        private readonly IProductionPlanTrendDataProvider _trendDataProvider;
        private readonly IProductionStageStandardService _stageStandardService;
        private readonly IProductionDeliveryDataProvider _deliveryDataProvider;
        private readonly IProductionDeliveryTargetService _deliveryTargetService;
        private readonly IProductionGoldLossTargetService _goldLossTargetService;
        private readonly IProductionWorkerLookupService _workerLookupService;
        private readonly IProductionMaterialGemDataProvider _materialGemDataProvider;

        public ProductionInsightService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IProductionPlanWipHelper wipHelper,
            IExecutiveReportService executiveReportService,
            IProductionPlanTrendDataProvider trendDataProvider,
            IProductionStageStandardService stageStandardService,
            IProductionDeliveryDataProvider deliveryDataProvider,
            IProductionDeliveryTargetService deliveryTargetService,
            IProductionGoldLossTargetService goldLossTargetService,
            IProductionWorkerLookupService workerLookupService,
            IProductionMaterialGemDataProvider materialGemDataProvider)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _wipHelper = wipHelper;
            _executiveReportService = executiveReportService;
            _trendDataProvider = trendDataProvider;
            _stageStandardService = stageStandardService;
            _deliveryDataProvider = deliveryDataProvider;
            _deliveryTargetService = deliveryTargetService;
            _goldLossTargetService = goldLossTargetService;
            _workerLookupService = workerLookupService;
            _materialGemDataProvider = materialGemDataProvider;
        }

        public async Task<Wip.Response> Wip(Wip.Request request)
        {
            var now = DateTime.UtcNow;
            var staleDays = request.StaleDays > 0 ? request.StaleDays : ProductionInsightThresholds.DefaultStaleDays;
            var riskWindowDays = request.RiskWindowDays > 0 ? request.RiskWindowDays : ProductionInsightThresholds.DefaultRiskWindowDays;
            var growthThresholdPercent = request.GrowthThresholdPercent > 0 ? request.GrowthThresholdPercent : ProductionInsightThresholds.DefaultGrowthThresholdPercent;

            // ช่วงของ report.flow/FC_BOTTLENECK/WIP_DEPT_GROWING เท่านั้น — กฎ snapshot อื่นยึด "ตอนนี้" เสมอ
            var rangeStart = request.Start?.UtcDateTime ?? now.AddDays(-ProductionInsightThresholds.DefaultFlowRangeDays);
            var rangeEnd = request.End?.UtcDateTime ?? now;

            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var meltedOpenCount = await _wipHelper.GetMeltedOpenCountAsync();

            var openCount = openPlans.Count;
            var staleCount = 0;
            var becomingStaleCount = 0;
            var overdueCount = 0;
            var dueSoonCount = 0;
            var staleByDept = new Dictionary<string, int>();
            var stalePlanIds = new HashSet<int>();
            var dueSoonWindowEnd = now.AddDays(riskWindowDays);

            foreach (var plan in openPlans)
            {
                var lastMove = ProductionPlanDepartments.LastMoveOf(plan, lastMoveDates);
                var ageDays = (now - lastMove).TotalDays;

                if (ageDays >= staleDays)
                {
                    staleCount++;
                    stalePlanIds.Add(plan.Id);

                    var deptKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status);
                    if (deptKey != null)
                    {
                        staleByDept.TryGetValue(deptKey, out var currentCount);
                        staleByDept[deptKey] = currentCount + 1;
                    }
                }
                else if (ageDays >= staleDays - riskWindowDays)
                {
                    becomingStaleCount++;
                }

                // เกินกำหนด — นิยามเดียวกับ IsOverPlan ของ PlanService.GetDailyReport (เกณฑ์ "เกินกำหนด" ในแดชบอร์ดเดิม):
                // request_date < now และยังไม่เข้า successStatus (Completed/Melted กรองออกไปตั้งแต่ open plans แล้ว, เหลือ WaitCVD/CVD)
                var isNotYetDone = plan.Status != ProductionPlanStatusConst.WaitCVD && plan.Status != ProductionPlanStatusConst.CVD;
                if (isNotYetDone && plan.RequestDate < now)
                {
                    overdueCount++;
                }

                if (plan.RequestDate >= now && plan.RequestDate <= dueSoonWindowEnd && plan.Status < ProductionPlanStatusConst.Plated)
                {
                    dueSoonCount++;
                }
            }

            // ---- Flow (inflow/outflow ในช่วง [rangeStart, rangeEnd] ต่อแผนก — ค่า default คือ 90 วันล่าสุดเหมือนเดิม) ----
            // buffer โหลด header ย้อนก่อน rangeStart เท่ากับความยาวช่วงเอง (เดิม 90/180 คือ x2 พอดี) เผื่อหา prev ของ transition แรกในช่วง
            var rangeSpan = rangeEnd > rangeStart ? rangeEnd - rangeStart : TimeSpan.Zero;
            var headerWindowStartUtc = rangeStart - rangeSpan;
            var headerRows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.CreateDate >= headerWindowStartUtc && h.CreateDate <= rangeEnd && h.ProductionPlan.IsActive == true)
                .Select(h => new ProductionPlanFlowCalculator.HeaderRow
                {
                    ProductionPlanId = h.ProductionPlanId,
                    Status = h.Status,
                    CreateDate = h.CreateDate
                })
                .ToListAsync();

            // แผนที่ถือกำเนิดในช่วงนี้ (ไม่ต้อง buffer — ใช้ rangeStart ตรงๆ) → inflow เข้าแผนกออกแบบ
            var planCreationRows = await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.CreateDate >= rangeStart && p.CreateDate <= rangeEnd)
                .Select(p => new ProductionPlanFlowCalculator.PlanCreationRow { ProductionPlanId = p.Id, CreateDate = p.CreateDate })
                .ToListAsync();

            var flowByDept = ProductionPlanFlowCalculator.ComputeFlow(headerRows, planCreationRows, ProductionPlanDepartments.DepartmentKeyOf, rangeStart);

            // ---- report.departments — reuse ExecutiveReport/ProductionWip ตรงๆ (fixed 30d/180d เหมือนเดิม) ----
            var executiveWip = await _executiveReportService.ProductionWip(new ExecutiveProductionWip.Request());
            var departmentsReport = executiveWip.Departments.Select(d => new Wip.DepartmentReportItem
            {
                Key = d.Key,
                Total = d.Total,
                Moved30d = d.Moved30d,
                Moved30to180d = d.Moved30to180d,
                Stale180d = d.Stale180d
            }).ToList();

            var flowReport = ProductionPlanDepartments.Departments.Select(d =>
            {
                var flow = flowByDept.TryGetValue(d.Key, out var f) ? f : (Inflow: 0, Outflow: 0);
                return new Wip.FlowReportItem
                {
                    Key = d.Key,
                    Inflow90d = flow.Inflow,
                    Outflow90d = flow.Outflow,
                    Inflow = flow.Inflow,
                    Outflow = flow.Outflow,
                    Net = flow.Inflow - flow.Outflow
                };
            }).ToList();

            // ---- WIP_DEPT_GROWING (ใช้ WipTrend evaluator แค่ 2 จุดเวลา start/end ไม่ต้องทำ bucket) ----
            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var growthSnapshots = ProductionPlanTrendEvaluator.ComputeWipCountsAtTimes(
                trendData.Plans, trendData.Headers, ProductionPlanDepartments.DepartmentKeyOf, new[] { rangeStart, rangeEnd });
            var growthStartSnapshot = growthSnapshots[rangeStart];
            var growthEndSnapshot = growthSnapshots[rangeEnd];

            // ---- Rules ----
            var problems = new List<Wip.Finding>();
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWipStale(staleCount, openCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWipOverdue(overdueCount, openCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWipDeptStaleTop(staleByDept, staleCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWipMeltedOpen(meltedOpenCount));

            foreach (var dept in ProductionPlanDepartments.Departments)
            {
                var startWip = growthStartSnapshot.ByDept.GetValueOrDefault(dept.Key, 0);
                var endWip = growthEndSnapshot.ByDept.GetValueOrDefault(dept.Key, 0);
                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWipDeptGrowing(dept.Key, startWip, endWip, growthThresholdPercent));
            }

            var forecasts = new List<Wip.Finding>();
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcBecomingStale(becomingStaleCount, riskWindowDays));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcDueSoonAtRisk(dueSoonCount, riskWindowDays));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcBottleneck(flowByDept));

            // ---- Stage lead-time rules (ใช้ standard ที่ "บันทึกไว้" เท่านั้น ไม่รับ draft) ----
            // bucket แบบ week คงที่ (ไม่ผูกกับ request) เพื่อให้มี series พอเช็ค 3 bucket ล่าสุดของ FC_STAGE_LEADTIME_RISING
            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();
            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);
            var leadTimeBucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(rangeStart, rangeEnd, "week").Select(b => b.End).ToList();

            foreach (var dept in ProductionPlanDepartments.Departments)
            {
                var hasSaved = savedStandards.TryGetValue(dept.Key, out var savedStd);
                var standardDays = hasSaved ? savedStd.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays;
                var effectiveFrom = hasSaved ? (DateTime?)savedStd.EffectiveFrom : null;

                // dept.StatusIds[0] = สถานะ "รอ" ของแผนกนี้ (ถ้ามี — ออกแบบไม่มี) — นับแผนที่ status ปัจจุบันตรงเป๊ะ
                var waitStatus = dept.StatusIds.Length >= 2 ? dept.StatusIds[0] : (int?)null;
                var currentWaitingCount = waitStatus.HasValue ? trendData.Plans.Count(p => p.Status == waitStatus.Value) : 0;

                var deptVisits = visits.Where(v => v.DeptKey == dept.Key).ToList();
                var stats = ProductionStageLeadTimeEvaluator.BuildDepartmentStats(
                    deptVisits, standardDays, "saved", effectiveFrom, rangeStart, rangeEnd, leadTimeBucketEnds, now,
                    ProductionInsightThresholds.DefaultAbnormalDwellMultiplier, currentWaitingCount, stalePlanIds);

                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateStageOverStandard(dept.Key, stats.Median.Total, standardDays));
                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateStageAbnormalDwell(
                    dept.Key, stats.AbnormalCount, (double)(standardDays * ProductionInsightThresholds.DefaultAbnormalDwellMultiplier)));
                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateStageWaitDominant(
                    dept.Key, stats.SplitSampleCount, stats.Median.Wait, stats.Median.Work));
                AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcStageLeadtimeRising(
                    dept.Key, stats.Series.Select(s => (s.Count, s.MedianTotal)).ToList()));
            }

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            // top-level status ตาม contract มีแค่ critical|warning|ok (ไม่มี info) — ต่างจาก Finding.Severity ที่มี info ได้
            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Wip.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                Report = new Wip.ReportData
                {
                    Departments = departmentsReport,
                    Flow = flowReport,
                    OpenCount = openCount,
                    OverdueCount = overdueCount,
                    DueSoonAtRiskCount = dueSoonCount,
                    BecomingStaleCount = becomingStaleCount,
                    MeltedOpenCount = meltedOpenCount
                }
            };
        }

        public async Task<WipTrend.Response> WipTrend(WipTrend.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucket = string.Equals(request.Bucket, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

            var trendData = await _trendDataProvider.GetTrendDataAsync();

            return ProductionPlanTrendEvaluator.EvaluateTrend(
                trendData.Plans,
                trendData.Headers,
                ProductionPlanDepartments.DepartmentKeyOf,
                ProductionPlanDepartments.Departments,
                start,
                end,
                bucket);
        }

        // contract เดียวกับ ExecutiveReport/StalePlans เป๊ะๆ — delegate ตรงๆ ให้ผู้ใช้ที่มีแค่ production:view เข้าถึงได้
        public async Task<DataSourceResult> StalePlans(StalePlans.Request request)
        {
            return await _executiveReportService.StalePlans(request);
        }

        public async Task<DataSourceResult> DueRiskPlans(DueRiskPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var mode = string.IsNullOrWhiteSpace(request.Mode) ? "overdue" : request.Mode.Trim().ToLowerInvariant();
            var riskWindowDays = request.RiskWindowDays > 0 ? request.RiskWindowDays : ProductionInsightThresholds.DefaultRiskWindowDays;

            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var statusNames = await _wipHelper.GetStatusNamesAsync();

            IEnumerable<OpenPlanRow> filtered;
            if (mode == "duesoon")
            {
                var windowEnd = now.AddDays(riskWindowDays);
                filtered = openPlans.Where(p => p.RequestDate >= now && p.RequestDate <= windowEnd && p.Status < ProductionPlanStatusConst.Plated);
            }
            else
            {
                filtered = openPlans.Where(p => p.RequestDate < now
                    && p.Status != ProductionPlanStatusConst.WaitCVD
                    && p.Status != ProductionPlanStatusConst.CVD);
            }

            var items = filtered.Select(p =>
            {
                var lastMove = ProductionPlanDepartments.LastMoveOf(p, lastMoveDates);
                return new DueRiskPlans.Item
                {
                    PlanId = p.Id,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    StatusId = p.Status,
                    StatusName = statusNames.TryGetValue(p.Status, out var name) ? name : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(p.Status),
                    CreateDate = p.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    DueDate = p.RequestDate,
                    DaysToDue = (int)(p.RequestDate - now).TotalDays
                };
            }).ToList();

            IEnumerable<DueRiskPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderBy(x => x.DaysToDue);
            }

            var dataSource = ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);

            // enrich เฉพาะหน้าปัจจุบันหลัง paging (paging skill: post-processing pattern) — ไม่ query ทั้งตาราง
            var pageItems = dataSource.Data?.Cast<DueRiskPlans.Item>().ToList() ?? new List<DueRiskPlans.Item>();
            if (pageItems.Count > 0)
            {
                var plansById = openPlans.ToDictionary(p => p.Id);
                var pagePlans = pageItems.Select(i => plansById[i.PlanId]).ToList();
                var lastActionInfo = await _wipHelper.GetLastActionInfoAsync(pagePlans);

                foreach (var item in pageItems)
                {
                    if (!lastActionInfo.TryGetValue(item.PlanId, out var info)) continue;
                    item.LastUpdateBy = info.LastUpdateBy;
                    item.LastAction = info.LastAction;
                    item.LastActionRemark = info.LastActionRemark;
                    item.LastActionDate = info.LastActionDate;
                    item.Workers = info.Workers;
                    item.WorkerItems = info.WorkerItems.Select(w => new DueRiskPlans.WorkerItem { Code = w.Code, Name = w.Name, IsQueue = w.IsQueue }).ToList();
                }

                dataSource.Data = pageItems;
            }

            return dataSource;
        }

        public async Task<StageLeadTime.Response> StageLeadTime(StageLeadTime.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucket = string.Equals(request.Bucket, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);

            // stale plan ids (นิยามเดียวกับ WIP_STALE) — ตัดออกจาก AbnormalCount เท่านั้น (มี StalePlans table ของตัวเองแล้ว)
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIds = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));

            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();
            var draftByDept = request.DraftStandards?
                .Where(d => !string.IsNullOrWhiteSpace(d.DeptKey))
                .ToDictionary(d => d.DeptKey, d => d.StandardDays) ?? new Dictionary<string, decimal>();

            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, bucket).Select(b => b.End).ToList();

            var departmentsOut = new List<StageLeadTime.DepartmentLeadTimeData>();
            var standardByDeptForCapacity = new Dictionary<string, decimal>();

            foreach (var dept in ProductionPlanDepartments.Departments)
            {
                decimal standardDays;
                string source;
                DateTime? effectiveFrom;
                var hasSaved = savedStandards.TryGetValue(dept.Key, out var saved);

                if (draftByDept.TryGetValue(dept.Key, out var draftDays))
                {
                    standardDays = draftDays;
                    source = "draft";
                    effectiveFrom = hasSaved ? (DateTime?)saved.EffectiveFrom : null;
                }
                else if (hasSaved)
                {
                    standardDays = saved.StandardDays;
                    source = "saved";
                    effectiveFrom = saved.EffectiveFrom;
                }
                else
                {
                    // ไม่ควรเกิดจริง (seed migration ใส่ครบทุกแผนกแล้ว) — กันพังเฉยๆ
                    standardDays = ProductionInsightThresholds.DefaultStageStandardDays;
                    source = "saved";
                    effectiveFrom = null;
                }

                standardByDeptForCapacity[dept.Key] = standardDays;

                var waitStatus = dept.StatusIds.Length >= 2 ? dept.StatusIds[0] : (int?)null;
                var currentWaitingCount = waitStatus.HasValue ? trendData.Plans.Count(p => p.Status == waitStatus.Value) : 0;

                var deptVisits = visits.Where(v => v.DeptKey == dept.Key).ToList();
                var stats = ProductionStageLeadTimeEvaluator.BuildDepartmentStats(
                    deptVisits, standardDays, source, effectiveFrom, start, end, bucketEnds, now,
                    ProductionInsightThresholds.DefaultAbnormalDwellMultiplier, currentWaitingCount, stalePlanIds);
                stats.Key = dept.Key;
                departmentsOut.Add(stats);
            }

            var activeWipByDept = ComputeActiveWipByDept(openPlans, stalePlanIds);
            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(
                visits, ProductionPlanDepartments.Departments, standardByDeptForCapacity, activeWipByDept, start, end);

            return new StageLeadTime.Response
            {
                Departments = departmentsOut,
                Capacity = capacity,
                SplitDataSince = trendData.MinReceiveDate
            };
        }

        public async Task<DataSourceResult> AbnormalDwellPlans(AbnormalDwellPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var multiplier = request.Multiplier > 0 ? request.Multiplier : ProductionInsightThresholds.DefaultAbnormalDwellMultiplier;

            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);
            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();

            // visit ที่ยังไม่จบ = แผนที่อยู่ใน open plans อยู่แล้วเสมอ (ยังไม่ 100/500) — ใช้ชุดข้อมูลเดียวกับ
            // endpoint อื่นๆ (StalePlans/DueRiskPlans) เพื่อ enrich รายละเอียด + lastUpdateBy/workers แบบเดียวกัน
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var statusNames = await _wipHelper.GetStatusNamesAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var plansById = openPlans.ToDictionary(p => p.Id);

            // stale plan (นิยามเดียวกับ WIP_STALE) ไม่นับในนี้ — ครอบคลุมโดย StalePlans table ของตัวเองแล้ว
            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIds = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));

            var ongoing = visits.Where(v => !v.End.HasValue && !stalePlanIds.Contains(v.PlanId)).ToList();
            if (request.DepartmentKeys != null && request.DepartmentKeys.Length > 0)
            {
                var deptSet = new HashSet<string>(request.DepartmentKeys);
                ongoing = ongoing.Where(v => deptSet.Contains(v.DeptKey)).ToList();
            }

            var abnormal = ongoing.Where(v =>
            {
                var standard = savedStandards.TryGetValue(v.DeptKey, out var s) ? s.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays;
                var thresholdDays = (double)(standard * multiplier);
                return (now - v.Start).TotalDays > thresholdDays;
            }).ToList();

            var items = new List<AbnormalDwellPlans.Item>();
            foreach (var visit in abnormal)
            {
                // เผื่อแผนเปลี่ยนสถานะไปแล้วระหว่างคำนวณ (edge case, ข้อมูล 2 ชุดโหลดคนละเวลากันเล็กน้อย)
                if (!plansById.TryGetValue(visit.PlanId, out var plan)) continue;

                var lastMove = ProductionPlanDepartments.LastMoveOf(plan, lastMoveDates);
                var standard = savedStandards.TryGetValue(visit.DeptKey, out var sd) ? sd.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays;

                items.Add(new AbnormalDwellPlans.Item
                {
                    PlanId = plan.Id,
                    Wo = plan.Wo,
                    WoNumber = plan.WoNumber,
                    WoText = plan.WoText,
                    Mold = plan.Mold,
                    ProductNumber = plan.ProductNumber,
                    ProductName = plan.ProductName,
                    ProductQty = plan.ProductQty,
                    StatusId = plan.Status,
                    StatusName = statusNames.TryGetValue(plan.Status, out var name) ? name : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status),
                    CreateDate = plan.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    DeptKey = visit.DeptKey,
                    DaysInDept = Math.Round(visit.TotalDays, 1),
                    WaitDays = visit.HasSplit ? Math.Round(visit.WaitDays!.Value, 1) : (double?)null,
                    WorkDays = visit.HasSplit ? Math.Round(visit.WorkDays!.Value, 1) : (double?)null,
                    StandardDays = standard
                });
            }

            IEnumerable<AbnormalDwellPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysInDept);
            }

            var dataSource = ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);

            var pageItems = dataSource.Data?.Cast<AbnormalDwellPlans.Item>().ToList() ?? new List<AbnormalDwellPlans.Item>();
            if (pageItems.Count > 0)
            {
                var pagePlans = pageItems.Select(i => plansById[i.PlanId]).ToList();
                var lastActionInfo = await _wipHelper.GetLastActionInfoAsync(pagePlans);

                foreach (var item in pageItems)
                {
                    if (!lastActionInfo.TryGetValue(item.PlanId, out var info)) continue;
                    item.LastUpdateBy = info.LastUpdateBy;
                    item.LastAction = info.LastAction;
                    item.LastActionRemark = info.LastActionRemark;
                    item.LastActionDate = info.LastActionDate;
                    item.Workers = info.Workers;
                    item.WorkerItems = info.WorkerItems.Select(w => new StalePlans.WorkerItem { Code = w.Code, Name = w.Name, IsQueue = w.IsQueue }).ToList();
                }

                dataSource.Data = pageItems;
            }

            return dataSource;
        }

        public async Task<List<StageStandards.Item>> GetStageStandards()
        {
            var current = await _stageStandardService.GetCurrentStandardsAsync();

            return ProductionPlanDepartments.Departments.Select(d =>
                current.TryGetValue(d.Key, out var row)
                    ? new StageStandards.Item { DeptKey = d.Key, StandardDays = row.StandardDays, EffectiveFrom = row.EffectiveFrom, CreateBy = row.CreateBy, Remark = row.Remark }
                    : new StageStandards.Item { DeptKey = d.Key, StandardDays = ProductionInsightThresholds.DefaultStageStandardDays, EffectiveFrom = default, CreateBy = "system", Remark = "ไม่มีข้อมูลในตาราง ใช้ค่า default" }
            ).ToList();
        }

        public async Task<List<StageStandards.Item>> GetStageStandardHistory(string deptKey)
        {
            var rows = await _stageStandardService.GetHistoryAsync(deptKey);
            return rows.Select(r => new StageStandards.Item
            {
                DeptKey = r.DeptKey,
                StandardDays = r.StandardDays,
                EffectiveFrom = r.EffectiveFrom,
                CreateBy = r.CreateBy,
                Remark = r.Remark
            }).ToList();
        }

        public async Task SaveStageStandards(SaveStageStandards.Request request)
        {
            var validDeptKeys = new HashSet<string>(ProductionPlanDepartments.Departments.Select(d => d.Key));

            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException("ต้องระบุอย่างน้อย 1 แผนก");
            }

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.DeptKey) || !validDeptKeys.Contains(item.DeptKey))
                {
                    throw new ArgumentException($"ไม่รู้จักแผนก '{item.DeptKey}'");
                }

                if (item.StandardDays <= 0 || item.StandardDays > 365)
                {
                    throw new ArgumentException($"มาตรฐานวันของแผนก '{item.DeptKey}' ต้องอยู่ระหว่าง 1-365 วัน");
                }
            }

            var items = request.Items.Select(i => (i.DeptKey, i.StandardDays)).ToList();
            await _stageStandardService.SaveAsync(items, request.Remark, CurrentUsername);
        }

        // ---- Delivery (ส่งงานตรงเวลา) ----

        // รวมข้อมูลที่ endpoint Delivery/DeliveryAtRiskPlans/DeliveryLatePlans/StuckAfterCostCardPlans ใช้ร่วมกัน
        // โหลดครั้งเดียวต่อ request (ไม่ query ซ้ำข้าม endpoint — ตาม instruction "one pass loads")
        private class DeliveryContext
        {
            public List<ProductionStageLeadTimeEvaluator.VisitRecord> Visits = null!;
            public Dictionary<int, ProductionStageLeadTimeEvaluator.VisitRecord> OngoingVisitByPlanId = null!;
            public Dictionary<string, double> MedianTotalByDept = null!;
            public double CostCardExitToDoneMedian;
            public HashSet<int> StalePlanIds = null!;
            public List<OpenPlanRow> OpenPlans = null!;
            public Dictionary<int, OpenPlanRow> OpenPlansById = null!;
            public Dictionary<int, DateTime> LastMoveDates = null!;
            public List<ProductionPlanTrendEvaluator.PlanTrendRow> AllPlans = null!;
            public List<ProductionPlanFlowCalculator.HeaderRow> Headers = null!;
            public Dictionary<int, DateTime> DoneDates = null!;
        }

        private async Task<DeliveryContext> LoadDeliveryContextAsync(DateTime now)
        {
            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var doneDates = await _deliveryDataProvider.GetDoneDatesAsync();
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();

            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIds = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));

            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);
            var ongoingVisitByPlanId = visits.Where(v => !v.End.HasValue).ToDictionary(v => v.PlanId);

            // median รวม (wait+work) ต่อแผนก ย้อนหลัง 90 วัน (DefaultFlowRangeDays) — ใช้คำนวณ projectedFinishDate
            var medianWindowStart = now.AddDays(-ProductionInsightThresholds.DefaultFlowRangeDays);
            var medianTotalByDept = new Dictionary<string, double>();
            foreach (var dept in ProductionPlanDepartments.Departments)
            {
                var deptExitedTotals = visits
                    .Where(v => v.DeptKey == dept.Key && v.End.HasValue && v.End.Value >= medianWindowStart && v.End.Value <= now)
                    .Select(v => v.TotalDays)
                    .ToList();
                medianTotalByDept[dept.Key] = ProductionStageLeadTimeEvaluator.Median(deptExitedTotals);
            }

            var costCardExitToDoneMedian = ProductionDeliveryEvaluator.ComputeCostCardExitToDoneMedian(visits, doneDates) ?? 0;

            return new DeliveryContext
            {
                Visits = visits,
                OngoingVisitByPlanId = ongoingVisitByPlanId,
                MedianTotalByDept = medianTotalByDept,
                CostCardExitToDoneMedian = costCardExitToDoneMedian,
                StalePlanIds = stalePlanIds,
                OpenPlans = openPlans,
                OpenPlansById = openPlans.ToDictionary(p => p.Id),
                LastMoveDates = lastMoveDates,
                AllPlans = trendData.Plans,
                Headers = trendData.Headers,
                DoneDates = doneDates
            };
        }

        // at-risk = open, ไม่ stale, request_date อยู่ระหว่าง [now, now+horizon], projectedFinish > request_date
        // (เทียบวันที่ไทย) — reuse โดย Delivery() (เอาแค่ count) และ DeliveryAtRiskPlans() (เอา list เต็ม)
        private static List<(OpenPlanRow Plan, string? CurrentDeptKey, double DaysInCurrentDept, ProductionDeliveryEvaluator.AtRiskProjection Projection)>
            ComputeAtRiskList(DeliveryContext ctx, DateTime now, int riskHorizonDays, string[]? departmentKeysFilter)
        {
            var windowEnd = now.AddDays(riskHorizonDays);
            var deptFilter = departmentKeysFilter != null && departmentKeysFilter.Length > 0 ? new HashSet<string>(departmentKeysFilter) : null;
            var result = new List<(OpenPlanRow, string?, double, ProductionDeliveryEvaluator.AtRiskProjection)>();

            foreach (var plan in ctx.OpenPlans)
            {
                if (ctx.StalePlanIds.Contains(plan.Id)) continue;
                if (plan.RequestDate < now || plan.RequestDate > windowEnd) continue;

                var currentDeptKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status);
                if (deptFilter != null && (currentDeptKey == null || !deptFilter.Contains(currentDeptKey))) continue;
                if (!ctx.OngoingVisitByPlanId.TryGetValue(plan.Id, out var visit) || visit.DeptKey != currentDeptKey) continue;

                var projection = ProductionDeliveryEvaluator.ComputeAtRiskProjection(
                    currentDeptKey, visit.TotalDays, plan.RequestDate, now,
                    ProductionPlanDepartments.Departments, ctx.MedianTotalByDept, ctx.CostCardExitToDoneMedian);

                if (projection.ProjectedFinishDate.AddHours(7).Date <= plan.RequestDate.AddHours(7).Date) continue;

                result.Add((plan, currentDeptKey, visit.TotalDays, projection));
            }

            return result;
        }

        public async Task<Delivery.Response> Delivery(Delivery.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucket = string.Equals(request.Bucket, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";
            var riskHorizonDays = request.RiskHorizonDays > 0 ? request.RiskHorizonDays : ProductionInsightThresholds.DefaultRiskWindowDays;

            var ctx = await LoadDeliveryContextAsync(now);
            var customerNames = await _deliveryDataProvider.GetCustomerNamesAsync();

            var savedTarget = await _deliveryTargetService.GetCurrentTargetAsync();
            decimal targetPercent;
            string targetSource;
            DateTime? targetEffectiveFrom;
            if (request.DraftTargetPercent.HasValue)
            {
                targetPercent = request.DraftTargetPercent.Value;
                targetSource = "draft";
                targetEffectiveFrom = savedTarget?.EffectiveFrom;
            }
            else if (savedTarget != null)
            {
                targetPercent = savedTarget.TargetPercent;
                targetSource = "saved";
                targetEffectiveFrom = savedTarget.EffectiveFrom;
            }
            else
            {
                targetPercent = ProductionInsightThresholds.DefaultDeliveryTargetPercent;
                targetSource = "saved";
                targetEffectiveFrom = null;
            }

            // completed-in-range = done (transfer แรกเข้า 100 หรือ fallback completed_date) อยู่ใน [start,end]
            var completedInRange = new List<ProductionDeliveryEvaluator.CompletedPlanRow>();
            foreach (var plan in ctx.AllPlans)
            {
                if (plan.Status != ProductionPlanStatusConst.Completed) continue;
                if (!ctx.DoneDates.TryGetValue(plan.Id, out var doneDate)) continue;
                if (doneDate < start || doneDate > end) continue;

                completedInRange.Add(new ProductionDeliveryEvaluator.CompletedPlanRow
                {
                    CustomerNumber = plan.CustomerNumber,
                    CreateDate = plan.CreateDate,
                    RequestDate = plan.RequestDate,
                    DoneDate = doneDate
                });
            }

            var onTimeStats = ProductionDeliveryEvaluator.ComputeOnTimeStats(completedInRange);
            var suggestedLeadDays = ProductionDeliveryEvaluator.SuggestedLeadDays(onTimeStats.ActualLeadMedianDays);

            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, bucket).Select(b => b.End).ToList();
            var seriesRaw = ProductionDeliveryEvaluator.ComputeSeries(completedInRange, start, bucketEnds);
            var series = seriesRaw.Select(s => new Delivery.SeriesPoint
            {
                BucketEnd = s.BucketEnd,
                CompletedCount = s.Stats.CompletedCount,
                OnTimeCount = s.Stats.OnTimeCount,
                OnTimePercent = s.Stats.OnTimePercent,
                PlannedLeadMedianDays = s.Stats.PlannedLeadMedianDays,
                ActualLeadMedianDays = s.Stats.ActualLeadMedianDays
            }).ToList();

            var lateCustomersRaw = ProductionDeliveryEvaluator.ComputeLateCustomers(completedInRange);
            var lateCustomers = lateCustomersRaw.Select(c => new Delivery.LateCustomerItem
            {
                CustomerCode = c.CustomerCode,
                CustomerName = customerNames.TryGetValue(c.CustomerCode, out var name) ? name : null,
                CompletedCount = c.CompletedCount,
                LateCount = c.LateCount,
                LatePercent = c.LatePercent,
                LateMedianDays = c.LateMedianDays
            }).ToList();

            var openCount = ctx.OpenPlans.Count;
            var openOverdueCount = 0;
            var openOverdueActiveCount = 0;
            foreach (var plan in ctx.OpenPlans)
            {
                var isNotYetDone = plan.Status != ProductionPlanStatusConst.WaitCVD && plan.Status != ProductionPlanStatusConst.CVD;
                if (isNotYetDone && plan.RequestDate < now)
                {
                    openOverdueCount++;
                    if (!ctx.StalePlanIds.Contains(plan.Id)) openOverdueActiveCount++;
                }
            }

            var atRiskList = ComputeAtRiskList(ctx, now, riskHorizonDays, null);
            var atRiskCount = atRiskList.Count;

            var costCard95PlanIds = new HashSet<int>(ctx.Headers.Where(h => h.Status == ProductionPlanStatusConst.Price).Select(h => h.ProductionPlanId));
            var excludedForStuck = new HashSet<int> { ProductionPlanStatusConst.Completed, ProductionPlanStatusConst.Melted, ProductionPlanStatusConst.WaitCVD, ProductionPlanStatusConst.CVD };
            var stuckAfterCostCardCount = ctx.AllPlans.Count(p => costCard95PlanIds.Contains(p.Id) && !excludedForStuck.Contains(p.Status));

            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();
            var standardByDept = ProductionPlanDepartments.Departments.ToDictionary(
                d => d.Key,
                d => savedStandards.TryGetValue(d.Key, out var s) ? s.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays);
            var medianWindowStart = now.AddDays(-ProductionInsightThresholds.DefaultFlowRangeDays);
            var activeWipByDeptForDelivery = ComputeActiveWipByDept(ctx.OpenPlans, ctx.StalePlanIds);
            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(ctx.Visits, ProductionPlanDepartments.Departments, standardByDept, activeWipByDeptForDelivery, medianWindowStart, now);
            var bottleneckDeptKey = capacity.Current.BottleneckDept;

            var kpi = new Delivery.KpiData
            {
                CompletedCount = onTimeStats.CompletedCount,
                OnTimeCount = onTimeStats.OnTimeCount,
                OnTimePercent = onTimeStats.OnTimePercent,
                LateMedianDays = onTimeStats.LateMedianDays,
                PlannedLeadMedianDays = onTimeStats.PlannedLeadMedianDays,
                ActualLeadMedianDays = onTimeStats.ActualLeadMedianDays,
                SuggestedLeadDays = suggestedLeadDays,
                OpenCount = openCount,
                OpenOverdueCount = openOverdueCount,
                OpenOverdueActiveCount = openOverdueActiveCount,
                AtRiskCount = atRiskCount,
                StuckAfterCostCardCount = stuckAfterCostCardCount
            };

            var problems = new List<Wip.Finding>();
            var forecasts = new List<Wip.Finding>();

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateDlvOnTimeBelowTarget(onTimeStats.OnTimePercent, targetPercent, bottleneckDeptKey));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateDlvLeadUnderestimated(onTimeStats.PlannedLeadMedianDays, onTimeStats.ActualLeadMedianDays, suggestedLeadDays));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateDlvOpenOverdue(openOverdueActiveCount, openCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateDlvStuckAfterCostCard(stuckAfterCostCardCount));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcDlvAtRisk(atRiskCount, riskHorizonDays));

            var seriesForRule = seriesRaw.Select(s => (s.Stats.CompletedCount, s.Stats.OnTimePercent)).ToList();
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcDlvOnTimeDeclining(seriesForRule, bottleneckDeptKey));

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Delivery.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                TargetPercent = targetPercent,
                TargetSource = targetSource,
                TargetEffectiveFrom = targetEffectiveFrom,
                Kpi = kpi,
                Series = series,
                LateCustomers = lateCustomers
            };
        }

        public async Task<DataSourceResult> DeliveryAtRiskPlans(DeliveryAtRiskPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var riskHorizonDays = request.RiskHorizonDays > 0 ? request.RiskHorizonDays : ProductionInsightThresholds.DefaultRiskWindowDays;

            var ctx = await LoadDeliveryContextAsync(now);
            var statusNames = await _wipHelper.GetStatusNamesAsync();

            var atRiskRows = ComputeAtRiskList(ctx, now, riskHorizonDays, request.DepartmentKeys);

            var items = atRiskRows.Select(r =>
            {
                var lastMove = ProductionPlanDepartments.LastMoveOf(r.Plan, ctx.LastMoveDates);
                return new DeliveryAtRiskPlans.Item
                {
                    PlanId = r.Plan.Id,
                    Wo = r.Plan.Wo,
                    WoNumber = r.Plan.WoNumber,
                    WoText = r.Plan.WoText,
                    Mold = r.Plan.Mold,
                    ProductNumber = r.Plan.ProductNumber,
                    ProductName = r.Plan.ProductName,
                    ProductQty = r.Plan.ProductQty,
                    StatusId = r.Plan.Status,
                    StatusName = statusNames.TryGetValue(r.Plan.Status, out var name) ? name : null,
                    DepartmentKey = r.CurrentDeptKey,
                    CreateDate = r.Plan.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    RequestDate = r.Plan.RequestDate,
                    CurrentDeptKey = r.CurrentDeptKey,
                    DaysInCurrentDept = Math.Round(r.DaysInCurrentDept, 1),
                    RemainingDays = r.Projection.RemainingDays,
                    ProjectedFinishDate = r.Projection.ProjectedFinishDate,
                    ProjectedLateDays = r.Projection.ProjectedLateDays
                };
            }).ToList();

            IEnumerable<DeliveryAtRiskPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.ProjectedLateDays);
            }

            var dataSource = ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);

            var pageItems = dataSource.Data?.Cast<DeliveryAtRiskPlans.Item>().ToList() ?? new List<DeliveryAtRiskPlans.Item>();
            if (pageItems.Count > 0)
            {
                var pagePlans = pageItems.Select(i => ctx.OpenPlansById[i.PlanId]).ToList();
                var lastActionInfo = await _wipHelper.GetLastActionInfoAsync(pagePlans);

                foreach (var item in pageItems)
                {
                    if (!lastActionInfo.TryGetValue(item.PlanId, out var info)) continue;
                    item.LastUpdateBy = info.LastUpdateBy;
                    item.LastAction = info.LastAction;
                    item.LastActionRemark = info.LastActionRemark;
                    item.LastActionDate = info.LastActionDate;
                    item.Workers = info.Workers;
                    item.WorkerItems = info.WorkerItems.Select(w => new StalePlans.WorkerItem { Code = w.Code, Name = w.Name, IsQueue = w.IsQueue }).ToList();
                }

                dataSource.Data = pageItems;
            }

            return dataSource;
        }

        // ไม่เติม LastUpdateBy/LastAction/Workers (enrichment สำหรับแผนเปิด) — ดู comment บน DeliveryLatePlans.Item
        public async Task<DataSourceResult> DeliveryLatePlans(DeliveryLatePlans.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;

            var ctx = await LoadDeliveryContextAsync(now);
            var statusNames = await _wipHelper.GetStatusNamesAsync();

            var items = new List<DeliveryLatePlans.Item>();
            foreach (var plan in ctx.AllPlans)
            {
                if (plan.Status != ProductionPlanStatusConst.Completed) continue;
                if (!ctx.DoneDates.TryGetValue(plan.Id, out var doneDate)) continue;
                if (doneDate < start || doneDate > end) continue;
                if (ProductionDeliveryEvaluator.IsOnTimeThai(doneDate, plan.RequestDate)) continue;

                var lateDays = (doneDate.AddHours(7).Date - plan.RequestDate.AddHours(7).Date).TotalDays;
                var lastMove = ctx.LastMoveDates.TryGetValue(plan.Id, out var lm) ? lm : plan.CreateDate;

                items.Add(new DeliveryLatePlans.Item
                {
                    PlanId = plan.Id,
                    Wo = plan.Wo,
                    WoNumber = plan.WoNumber,
                    WoText = plan.WoText,
                    Mold = plan.Mold,
                    ProductNumber = plan.ProductNumber,
                    ProductName = plan.ProductName,
                    ProductQty = plan.ProductQty,
                    StatusId = plan.Status,
                    StatusName = statusNames.TryGetValue(plan.Status, out var name) ? name : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status),
                    CreateDate = plan.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    RequestDate = plan.RequestDate,
                    DoneDate = doneDate,
                    LateDays = Math.Round(lateDays, 1)
                });
            }

            IEnumerable<DeliveryLatePlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.LateDays);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // "เคยเข้า" = มี active header Status=Price(95) อย่างน้อย 1 ครั้ง (เอาครั้งล่าสุดถ้าเข้าซ้ำ) — ไม่เคยถึง 100
        // และสถานะปัจจุบันไม่ใช่ 500/84/85 (ดู FACTS ในสเปก) — ไม่เติม LastUpdateBy/LastAction/Workers เช่นกัน
        public async Task<DataSourceResult> StuckAfterCostCardPlans(StuckAfterCostCardPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var ctx = await LoadDeliveryContextAsync(now);
            var statusNames = await _wipHelper.GetStatusNamesAsync();

            var excluded = new HashSet<int> { ProductionPlanStatusConst.Completed, ProductionPlanStatusConst.Melted, ProductionPlanStatusConst.WaitCVD, ProductionPlanStatusConst.CVD };

            var costCardDateByPlan = ctx.Headers
                .Where(h => h.Status == ProductionPlanStatusConst.Price)
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.Max(h => h.CreateDate));

            var items = new List<StuckAfterCostCardPlans.Item>();
            foreach (var plan in ctx.AllPlans)
            {
                if (excluded.Contains(plan.Status)) continue;
                if (!costCardDateByPlan.TryGetValue(plan.Id, out var costCardDate)) continue;

                var lastMove = ctx.LastMoveDates.TryGetValue(plan.Id, out var lm) ? lm : plan.CreateDate;

                items.Add(new StuckAfterCostCardPlans.Item
                {
                    PlanId = plan.Id,
                    Wo = plan.Wo,
                    WoNumber = plan.WoNumber,
                    WoText = plan.WoText,
                    Mold = plan.Mold,
                    ProductNumber = plan.ProductNumber,
                    ProductName = plan.ProductName,
                    ProductQty = plan.ProductQty,
                    StatusId = plan.Status,
                    StatusName = statusNames.TryGetValue(plan.Status, out var name) ? name : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status),
                    CreateDate = plan.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    CostCardDate = costCardDate,
                    DaysSinceCostCard = Math.Round((now - costCardDate).TotalDays, 1)
                });
            }

            IEnumerable<StuckAfterCostCardPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysSinceCostCard);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        public async Task<DeliveryTarget.Item?> GetDeliveryTarget()
        {
            var current = await _deliveryTargetService.GetCurrentTargetAsync();
            return current == null ? null : new DeliveryTarget.Item
            {
                TargetPercent = current.TargetPercent,
                EffectiveFrom = current.EffectiveFrom,
                CreateBy = current.CreateBy,
                Remark = current.Remark
            };
        }

        public async Task<List<DeliveryTarget.Item>> GetDeliveryTargetHistory()
        {
            var rows = await _deliveryTargetService.GetHistoryAsync();
            return rows.Select(r => new DeliveryTarget.Item
            {
                TargetPercent = r.TargetPercent,
                EffectiveFrom = r.EffectiveFrom,
                CreateBy = r.CreateBy,
                Remark = r.Remark
            }).ToList();
        }

        public async Task SaveDeliveryTarget(SaveDeliveryTarget.Request request)
        {
            if (request.TargetPercent < 0 || request.TargetPercent > 100)
            {
                throw new ArgumentException("เป้าหมาย % ต้องอยู่ระหว่าง 0-100");
            }

            if (string.IsNullOrWhiteSpace(request.Remark))
            {
                throw new ArgumentException("ต้องระบุหมายเหตุ");
            }

            await _deliveryTargetService.SaveAsync(request.TargetPercent, request.Remark, CurrentUsername);
        }

        // ---- Gold Loss (ทองและ Loss) ----
        // รอบแก้ไข 2026-10-01: เพิ่มมิติ metal ('GOLD'|'SILVER') — ใบปนทอง/เงิน ราคาต่างกันมาก คำนวณทีละโลหะ
        // เท่านั้น (เลือกจาก request.Metal) และ exclude แผนทดสอบ (TEST) ออกจาก coverage/uncovered ให้ตรงกัน

        private static bool IsValidMetal(string? metal) =>
            metal == ProductionGoldLossEvaluator.MetalGold || metal == ProductionGoldLossEvaluator.MetalSilver;

        // scope='SLIP' — worker_type ของใบ gold loss (50 ช่างแต่ง, 80 ช่างฝัง)
        private static decimal DefaultSlipGoldLossTargetPercent(int workerType, string metal)
        {
            if (metal == ProductionGoldLossEvaluator.MetalSilver)
            {
                return workerType == ProductionGoldLossEvaluator.WorkerTypeTang
                    ? ProductionInsightThresholds.DefaultGoldLossTargetPercentTangSilver
                    : ProductionInsightThresholds.DefaultGoldLossTargetPercentSettingSilver;
            }

            return workerType == ProductionGoldLossEvaluator.WorkerTypeTang
                ? ProductionInsightThresholds.DefaultGoldLossTargetPercentTangGold
                : ProductionInsightThresholds.DefaultGoldLossTargetPercentSettingGold;
        }

        // scope='STAGE' — worker_type ในที่นี้คือสถานะทำงานของแผนก (60 ขัดมัน/rawPolish, 80 ฝัง/setting,
        // 90 ชุบ/plating) ไม่ใช่ worker_type ของใบ gold loss — ตรงกับค่า seed migration 20261001_03
        private static decimal DefaultStageGoldLossTargetPercent(int deptWorkerType, string metal)
        {
            var isSilver = metal == ProductionGoldLossEvaluator.MetalSilver;
            return deptWorkerType switch
            {
                60 => 3.2m,
                80 => isSilver ? 2.4m : 4.3m,
                90 => isSilver ? 4.7m : 2.1m,
                _ => 0m
            };
        }

        // แผนทดสอบ (TEST) — exclude ออกจาก coverage/uncovered ทั้งคู่เพื่อให้ตัวเลขตรงกัน (เช็คที่ wo/ชื่อสินค้า/
        // รหัสสินค้า/รหัสลูกค้า มีคำว่า "TEST" ปนอยู่ — case-insensitive)
        private static IQueryable<TbtProductionPlanStatusDetail> ExcludeTestPlans(IQueryable<TbtProductionPlanStatusDetail> query)
        {
            return query.Where(d =>
                !(d.Header.ProductionPlan.Wo != null && d.Header.ProductionPlan.Wo.ToUpper().Contains("TEST"))
                && !(d.Header.ProductionPlan.ProductName != null && d.Header.ProductionPlan.ProductName.ToUpper().Contains("TEST"))
                && !(d.Header.ProductionPlan.ProductNumber != null && d.Header.ProductionPlan.ProductNumber.ToUpper().Contains("TEST"))
                && !(d.Header.ProductionPlan.CustomerNumber != null && d.Header.ProductionPlan.CustomerNumber.ToUpper().Contains("TEST")));
        }

        // metal filter แบบ SQL-translatable (ไม่เรียก ProductionGoldLossEvaluator.ClassifyMetal ตรงๆ เพราะ EF
        // แปล method เรียกเองของเราไม่ได้) — ตรรกะเดียวกัน: Gold == "SV" (case-insensitive) = เงิน, อื่นๆ/null = ทอง
        private static IQueryable<TbtProductionPlanStatusDetail> FilterByMetal(IQueryable<TbtProductionPlanStatusDetail> query, string metal)
        {
            var wantSilver = metal == ProductionGoldLossEvaluator.MetalSilver;
            return query.Where(d => ((d.Gold != null && d.Gold.Trim().ToUpper() == "SV") == wantSilver));
        }

        public async Task<Gold.Response> Gold(Gold.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucket = string.Equals(request.Bucket, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";
            var rangeDays = Math.Max((end - start).TotalDays, 1);
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;

            var includeTang = request.WorkerTypes == null || request.WorkerTypes.Length == 0
                || request.WorkerTypes.Contains(ProductionGoldLossEvaluator.WorkerTypeTang);
            var includeSetting = request.WorkerTypes == null || request.WorkerTypes.Length == 0
                || request.WorkerTypes.Contains(ProductionGoldLossEvaluator.WorkerTypeSetting);
            var codeSet = request.WorkerCodes != null && request.WorkerCodes.Length > 0
                ? new HashSet<string>(request.WorkerCodes, StringComparer.OrdinalIgnoreCase)
                : null;

            var savedTargets = await _goldLossTargetService.GetCurrentTargetsAsync();
            // Gold() ใช้เฉพาะ scope=SLIP — รายการ draft ที่ส่ง scope=STAGE มาด้วย (เช่นจากหน้าแก้เป้าหมายรวม) ถูก
            // กรองทิ้งเงียบๆ ตรงนี้ ไม่ error (ดู report)
            var draftByType = request.DraftTargets?
                .Where(d => (d.WorkerType == ProductionGoldLossEvaluator.WorkerTypeTang || d.WorkerType == ProductionGoldLossEvaluator.WorkerTypeSetting)
                    && IsValidMetal(d.Metal) && (string.IsNullOrEmpty(d.Scope) || d.Scope == "SLIP"))
                .ToDictionary(d => (d.WorkerType, d.Metal), d => d.TargetPercent) ?? new Dictionary<(int, string), decimal>();

            var targets = new List<Gold.TargetItem>();
            var targetPercentByType = new Dictionary<int, decimal>();

            foreach (var wt in new[] { ProductionGoldLossEvaluator.WorkerTypeTang, ProductionGoldLossEvaluator.WorkerTypeSetting })
            {
                var key = ("SLIP", wt, metal);
                var hasSaved = savedTargets.TryGetValue(key, out var saved);
                decimal targetPercent;
                string source;
                DateTime? effectiveFrom;

                if (draftByType.TryGetValue((wt, metal), out var draft))
                {
                    targetPercent = draft;
                    source = "draft";
                    effectiveFrom = hasSaved ? (DateTime?)saved.EffectiveFrom : null;
                }
                else if (hasSaved)
                {
                    targetPercent = saved.TargetPercent;
                    source = "saved";
                    effectiveFrom = saved.EffectiveFrom;
                }
                else
                {
                    targetPercent = DefaultSlipGoldLossTargetPercent(wt, metal);
                    source = "saved";
                    effectiveFrom = null;
                }

                targetPercentByType[wt] = targetPercent;
                targets.Add(new Gold.TargetItem { WorkerType = wt, Metal = metal, TargetPercent = targetPercent, Source = source, EffectiveFrom = effectiveFrom });
            }

            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, bucket).Select(b => b.End).ToList();

            var kpiList = new List<Gold.KpiItem>();
            var seriesList = new List<Gold.SeriesItem>();
            var workerList = new List<Gold.WorkerItem>();
            var problems = new List<Wip.Finding>();
            var forecasts = new List<Wip.Finding>();

            if (includeTang)
            {
                var tangSlipsRaw = await _jewelryContext.TbtGoldLossTangSlip
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.RequestDateEnd != null && s.RequestDateEnd >= start && s.RequestDateEnd <= end)
                    .ToListAsync();

                var tangFiltered = codeSet != null ? tangSlipsRaw.Where(s => codeSet.Contains(s.WorkerCode)).ToList() : tangSlipsRaw;
                var tangItemsBySlip = await GetTangItemsBySlipAsync(tangFiltered.Select(s => s.Id).ToList());

                var tangRows = tangFiltered.Select(s => new ProductionGoldLossEvaluator.TangSlipRow
                {
                    SlipId = s.Id,
                    DocumentNo = s.DocumentNo,
                    WorkerCode = s.WorkerCode,
                    WorkerName = s.WorkerName,
                    RequestDateStart = s.RequestDateStart,
                    RequestDateEnd = s.RequestDateEnd,
                    ReturnedTotal = s.ReturnedTotal,
                    RawLoss = s.RawLoss,
                    AllowedLoss = s.AllowedLoss,
                    PricePerGram = s.PricePerGram,
                    TotalMoneyDiff = s.TotalMoneyDiff,
                    Items = tangItemsBySlip.TryGetValue(s.Id, out var its) ? its : new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>()
                }).ToList();

                var tangUnits = ProductionGoldLossEvaluator.NormalizeTang(tangRows).Where(u => u.Metal == metal).ToList();
                await ProcessGoldWorkerType(
                    ProductionGoldLossEvaluator.WorkerTypeTang, metal, tangUnits, targetPercentByType[ProductionGoldLossEvaluator.WorkerTypeTang],
                    start, end, bucketEnds, rangeDays, codeSet, kpiList, seriesList, workerList, problems, forecasts);
            }

            if (includeSetting)
            {
                var settingUnits = (await GetSettingUnitsAsync(start, end, codeSet)).Where(u => u.Metal == metal).ToList();
                await ProcessGoldWorkerType(
                    ProductionGoldLossEvaluator.WorkerTypeSetting, metal, settingUnits, targetPercentByType[ProductionGoldLossEvaluator.WorkerTypeSetting],
                    start, end, bucketEnds, rangeDays, codeSet, kpiList, seriesList, workerList, problems, forecasts);
            }

            // ---- Loss ตามใบงานรายแผนก (จ่าย-รับ) — เพิ่มกฎเข้า problems/forecasts/actions ชุดเดียวกันนี้
            // bucket บังคับเป็น month เสมอ (FC_GOLD_STAGE_RISING ระบุ "3 monthly buckets" ชัดเจน ไม่ขึ้นกับ
            // request.Bucket ของ Gold() ที่อาจเป็น week) ----
            var stageBucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, "month").Select(b => b.End).ToList();
            // scope=STAGE draft (what-if) ขับ GOLD_STAGE_ABOVE_TARGET/ACT_CHECK_STAGE แทนค่า saved — scope=SLIP
            // ยังขับกฎฝั่งใบ gold loss ตามเดิม (ดู draftByType ด้านบน ซึ่งกรองเฉพาะ SLIP)
            var stageDraftByWorkerTypeForGold = request.DraftTargets?
                .Where(d => d.Scope == "STAGE" && d.Metal == metal)
                .GroupBy(d => d.WorkerType)
                .ToDictionary(g => g.Key, g => g.Last().TargetPercent);
            var stageSummary = await ComputeGoldStageSummaryAsync(metal, start, end, stageBucketEnds, stageDraftByWorkerTypeForGold);

            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                var stats = stageSummary.DeptStatsByDept[deptKey];
                var targetPercent = stageSummary.TargetPercentByDept[deptKey];
                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldStageAboveTarget(deptKey, metal, stats.DiffPercent, targetPercent));

                var seriesPercents = stageSummary.SeriesByDept[deptKey].Select(s => s.DiffPercent).ToList();
                AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcGoldStageRising(deptKey, metal, seriesPercents));
            }

            // GOLD_STAGE_PENDING_RETURN/ACT_RECEIVE_PENDING ใช้เฉพาะของที่มีช่างจริงถือครอง (pendingWithWorker) —
            // ของที่ค้าง "คิว" (placeholder/ไม่มีช่าง) ไม่ใช่ของที่ "มีคนลืมคืน" จึงแยกไปเป็น GOLD_STAGE_QUEUED ต่างหาก
            var workerNamesForStage = await _workerLookupService.GetWorkerNamesAsync();
            var pendingOverRowsAll = ProductionGoldStageEvaluator.ComputePendingOverDays(
                stageSummary.AllRows, ProductionInsightThresholds.GoldStagePendingOlderThanDaysDefault, now);
            var pendingOverWithWorker = pendingOverRowsAll
                .Where(r => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(r.WorkerCode, workerNamesForStage))
                .ToList();
            var pendingOverCount = pendingOverWithWorker.Count;
            var pendingOverGram = pendingOverWithWorker.Sum(r => r.SendGram);
            var pendingTopDeptKey = pendingOverWithWorker
                .GroupBy(r => r.DeptKey)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldStagePendingReturn(metal, pendingOverCount, pendingOverGram, pendingTopDeptKey));

            // GOLD_STAGE_QUEUED — ของทั้งหมดที่ยังค้างคิวอยู่ตอนนี้ (ไม่จำกัดอายุ ต่างจาก PENDING_RETURN ที่กรอง
            // เฉพาะเกิน 14 วัน) รวมทุกแผนก — info level แค่แจ้งให้รู้สต็อกของที่ยังไม่ออก
            var queueCount = stageSummary.DeptStatsByDept.Values.Sum(d => d.PendingQueueCount);
            var queueGram = stageSummary.DeptStatsByDept.Values.Sum(d => d.PendingQueueGram);
            var queueTopDeptKey = stageSummary.DeptStatsByDept.Values
                .Where(d => d.PendingQueueCount > 0)
                .OrderByDescending(d => d.PendingQueueCount)
                .Select(d => d.DeptKey)
                .FirstOrDefault();
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldStageQueued(metal, queueCount, queueGram, queueTopDeptKey));

            // GOLD_STAGE_OUTLIER_JOBS นับเฉพาะ outlier ที่มีช่างจริงถือครอง (โทษได้) — ของ unassigned ยัง loss จริง
            // แต่หาตัวคนรับผิดชอบไม่ได้ ไม่ควรเอาไปรวมกับของที่มีคนรับผิดชอบ
            var totalOutliers = stageSummary.OutliersAttributedByDept.Values.Sum(v => v.Count);
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldStageOutlierJobs(metal, totalOutliers));

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            EnrichGoldActionParams(actions, kpiList);
            EnrichGoldStageActionParams(actions, stageSummary, pendingOverCount, pendingOverGram);
            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Gold.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                Targets = targets,
                Kpi = kpiList,
                Series = seriesList,
                Workers = workerList
            };
        }

        // โหลด item ของใบ tang (เฉพาะที่ active) ไว้จัดประเภทโลหะของทั้งใบ — คืน dictionary ว่างถ้า slipIds ว่าง
        private async Task<Dictionary<long, List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>>> GetTangItemsBySlipAsync(List<long> slipIds)
        {
            if (slipIds.Count == 0) return new Dictionary<long, List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>>();

            var rows = await _jewelryContext.TbtGoldLossTangSlipItem
                .AsNoTracking()
                .Where(it => it.IsActive && slipIds.Contains(it.SlipId))
                .Select(it => new { it.SlipId, it.Gold, it.GoldWeightSend, it.GoldWeightCheck })
                .ToListAsync();

            return rows
                .GroupBy(it => it.SlipId)
                .ToDictionary(g => g.Key, g => g.Select(it => new ProductionGoldLossEvaluator.TangSlipItemMetalRow
                {
                    Gold = it.Gold,
                    WeightSend = it.GoldWeightSend,
                    WeightCheck = it.GoldWeightCheck
                }).ToList());
        }

        // query setting item พร้อม metal ต่อ item ตรงๆ (ไม่ต้อง normalize โลหะระดับใบเหมือน tang) — reuse จาก
        // Gold()/GoldOverSlips() ทั้งคู่ (ขอบเขต [start,end] ตาม RequestDateEnd ของ header เท่านั้น ยังไม่กรอง metal)
        private async Task<List<ProductionGoldLossEvaluator.LossUnit>> GetSettingUnitsAsync(DateTime start, DateTime end, HashSet<string>? codeSet)
        {
            var settingQuery = _jewelryContext.TbtWorkerGoldLossSlipItem
                .AsNoTracking()
                .Where(i => i.IsActive && i.Header.IsActive && i.Header.RequestDateEnd >= start && i.Header.RequestDateEnd <= end);

            if (codeSet != null)
            {
                settingQuery = settingQuery.Where(i => codeSet.Contains(i.Header.WorkerCode));
            }

            var settingRows = await settingQuery
                .Select(i => new ProductionGoldLossEvaluator.SettingSlipItemRow
                {
                    SlipId = i.SlipId,
                    DocumentNo = i.Header.DocumentNo,
                    WorkerCode = i.Header.WorkerCode,
                    WorkerName = i.Header.WorkerName,
                    RequestDateStart = i.Header.RequestDateStart,
                    RequestDateEnd = i.Header.RequestDateEnd,
                    Gold = i.Gold,
                    GoldWeightCheck = i.GoldWeightCheck,
                    WeightLossAllowed = i.WeightLossAllowed,
                    WeightLossActual = i.WeightLossActual,
                    GoldLossPrice = i.GoldLossPrice,
                    MoneyDiff = i.MoneyDiff
                })
                .ToListAsync();

            return ProductionGoldLossEvaluator.NormalizeSetting(settingRows);
        }

        // ประมวลผล 1 ประเภทช่าง (tang/setting) — เติม kpi/series/workers + evaluate rule ทั้งหมดของประเภทนี้
        private async Task ProcessGoldWorkerType(
            int workerType,
            string metal,
            List<ProductionGoldLossEvaluator.LossUnit> units,
            decimal targetPercent,
            DateTime start,
            DateTime end,
            List<DateTime> bucketEnds,
            double rangeDays,
            HashSet<string>? codeSet,
            List<Gold.KpiItem> kpiList,
            List<Gold.SeriesItem> seriesList,
            List<Gold.WorkerItem> workerList,
            List<Wip.Finding> problems,
            List<Wip.Finding> forecasts)
        {
            var (coverageJobs, coverageTotal) = await ComputeGoldCoverageAsync(workerType, metal, start, end, codeSet);
            var kpi = ProductionGoldLossEvaluator.ComputeKpi(workerType, metal, units, targetPercent, coverageJobs, coverageTotal);
            kpiList.Add(MapGoldKpi(kpi));

            var series = ProductionGoldLossEvaluator.ComputeSeries(workerType, units, start, bucketEnds);
            seriesList.AddRange(series.Select(MapGoldSeries));

            var workers = ProductionGoldLossEvaluator.ComputeWorkers(workerType, units, targetPercent, start, bucketEnds);
            workerList.AddRange(workers.Select(MapGoldWorker));

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldExcessOverAllowance(workerType, metal, kpi.ExcessGram, kpi.ExcessMoney));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldLossAboveTarget(workerType, metal, kpi.LossPercent, targetPercent));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldAllowanceAboveTarget(workerType, metal, kpi.AllowedPercent, targetPercent));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldMostWorkersOver(workerType, metal, kpi.WorkersOverCount, kpi.WorkerCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldSlipCoverageLow(workerType, metal, kpi.CoverageJobs, kpi.CoverageTotalJobs));

            var offenderRows = workers.Select(w => (w.WorkerCode, w.WorkerName, w.OverBuckets, w.QualifyingBuckets)).ToList();
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateGoldRepeatOffender(workerType, metal, offenderRows));

            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcGoldExcessProjected(workerType, metal, kpi.ExcessGram, kpi.ExcessMoney, rangeDays));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcGoldLossRising(workerType, metal, series.Select(s => (s.SlipCount, s.LossPercent)).ToList()));
        }

        // coverage = จำนวนงาน (status_detail: header active, status=workerType, gold_weight_check>0, request_date
        // ในช่วง, ไม่ใช่แผนทดสอบ, โลหะตรงกับที่ขอ) ที่ "คุ้มครอง" แล้ว / จำนวนงานที่เข้าเงื่อนไขทั้งหมด — ใช้
        // เงื่อนไขชุดเดียวกับ GoldUncoveredJobs ทุกจุด (รวม BuildGoldCoveredExpr) เพื่อให้ตัวเลขตรงกันเป๊ะ
        //
        // "คุ้มครอง" (รอบแก้ไข 2026-10-01 รอบ 2 — prod พบ stamp อย่างเดียวนับได้แค่ 155/458 ของ setting ทั้งที่
        // อีก 303 jobs ก็มี slip item ตรงกันจริง แค่ item.production_plan_id เป็น null ตอนสร้างใบ ไม่เคย stamp
        // กลับ) = stamp != null OR มี active slip item ที่ match โดย:
        //   - ProductionPlanId ตรงกัน (ถ้า item มีค่า) หรือ
        //   - Wo+WoNumber ของ "แผนผลิตของ status_detail" ตรงกับ item.Wo/item.WoNumber (ถ้า item.ProductionPlanId
        //     เป็น null — กรณีนี้ยืนยันจาก prod ว่า match ครบ 303/303)
        //   AND Gold ตรงกัน AND วันที่ไทยของ JobDate/RequestDate ตรงกัน (ยืนยันจาก prod ว่าให้ผลเหมือนกับเทียบ
        //   gold_weight_send ตรงกันเป๊ะ — เลือกใช้วันที่เพราะ index-friendly กว่า decimal equality)
        // ใช้ได้ทั้ง setting (tbt_worker_gold_loss_slip_item) และ tang (tbt_gold_loss_tang_slip_item — มี field
        // ชุดเดียวกันครบ จึงใช้กฎเดียวกัน แม้ tang จะ coverage สูงอยู่แล้ว 89% จาก stamp อย่างเดียว)
        private Expression<Func<TbtProductionPlanStatusDetail, bool>> BuildGoldCoveredExpr(int workerType)
        {
            if (workerType == ProductionGoldLossEvaluator.WorkerTypeSetting)
            {
                return d => d.WorkerGoldLossSlipId != null
                    || _jewelryContext.TbtWorkerGoldLossSlipItem.Any(item =>
                        item.IsActive && item.Header.IsActive
                        && ((item.ProductionPlanId != null && item.ProductionPlanId == d.ProductionPlanId)
                            || (item.ProductionPlanId == null && item.Wo == d.Header.ProductionPlan.Wo && item.WoNumber == d.Header.ProductionPlan.WoNumber))
                        && item.Gold == d.Gold
                        && item.JobDate.HasValue && d.RequestDate.HasValue
                        && item.JobDate.Value.AddHours(7).Date == d.RequestDate.Value.AddHours(7).Date);
            }

            return d => d.GoldLossTangSlipId != null
                || _jewelryContext.TbtGoldLossTangSlipItem.Any(item =>
                    item.IsActive && item.Header.IsActive
                    && ((item.ProductionPlanId != null && item.ProductionPlanId == d.ProductionPlanId)
                        || (item.ProductionPlanId == null && item.Wo == d.Header.ProductionPlan.Wo && item.WoNumber == d.Header.ProductionPlan.WoNumber))
                    && item.Gold == d.Gold
                    && item.JobDate.HasValue && d.RequestDate.HasValue
                    && item.JobDate.Value.AddHours(7).Date == d.RequestDate.Value.AddHours(7).Date);
        }

        // ห่อ body เดิมด้วย NOT โดยใช้ parameter ตัวเดียวกัน — ไม่ใช่ Expression.Invoke (EF แปลไม่ได้เสถียร)
        // จึงแปลเป็น SQL ได้ตรงไปตรงมาเหมือนเขียน !(...) เองตรงๆ
        private static Expression<Func<TbtProductionPlanStatusDetail, bool>> NegateDetailExpr(
            Expression<Func<TbtProductionPlanStatusDetail, bool>> expr)
        {
            return Expression.Lambda<Func<TbtProductionPlanStatusDetail, bool>>(Expression.Not(expr.Body), expr.Parameters);
        }

        private async Task<(int CoverageJobs, int CoverageTotalJobs)> ComputeGoldCoverageAsync(
            int workerType, string metal, DateTime start, DateTime end, HashSet<string>? codeSet)
        {
            var query = _jewelryContext.TbtProductionPlanStatusDetail
                .AsNoTracking()
                .Where(d => d.IsActive && d.Header.IsActive && d.Header.Status == workerType
                    && d.GoldWeightCheck > 0 && d.RequestDate != null && d.RequestDate >= start && d.RequestDate <= end);

            query = FilterByMetal(query, metal);
            query = ExcludeTestPlans(query);

            if (codeSet != null)
            {
                query = query.Where(d => (d.Header.WorkerCode != null && codeSet.Contains(d.Header.WorkerCode))
                    || (d.Worker != null && codeSet.Contains(d.Worker))
                    || (d.WorkerSub != null && codeSet.Contains(d.WorkerSub)));
            }

            var total = await query.CountAsync();
            var covered = await query.CountAsync(BuildGoldCoveredExpr(workerType));

            return (covered, total);
        }

        // ACT_REVIEW_ALLOWANCE / ACT_CHECK_WEIGHING ผูกกับ 2 finding code (อาจ trigger แค่ตัวเดียว) — ถ้าปล่อยให้
        // ดึง param จาก finding ที่ trigger อย่างเดียว ค่าที่ไม่ได้มาจาก finding นั้นจะเป็น 0 หลอกๆ (เช่น
        // allowedPercent=0% ทั้งที่จริงมีค่า แค่ GOLD_ALLOWANCE_ABOVE_TARGET ไม่ trigger) — แก้โดย "เติมทับ" ด้วย
        // ค่าจริงจาก kpi row ของ workerType+metal นั้นเสมอ (ground truth) หลัง BuildActions คืนค่ามาแล้ว
        private static void EnrichGoldActionParams(List<Wip.ActionItem> actions, List<Gold.KpiItem> kpiList)
        {
            foreach (var action in actions)
            {
                if (action.Code != "ACT_REVIEW_ALLOWANCE" && action.Code != "ACT_CHECK_WEIGHING") continue;

                var workerType = action.Params.TryGetValue("workerType", out var wtObj) && wtObj is int wt ? wt : (int?)null;
                var metal = action.Params.TryGetValue("metal", out var mObj) ? mObj as string : null;
                if (!workerType.HasValue || string.IsNullOrEmpty(metal)) continue;

                var kpi = kpiList.FirstOrDefault(k => k.WorkerType == workerType.Value && k.Metal == metal);
                if (kpi == null) continue;

                if (action.Code == "ACT_REVIEW_ALLOWANCE")
                {
                    action.Params["lossPercent"] = kpi.LossPercent.HasValue ? Math.Round(kpi.LossPercent.Value, 2, MidpointRounding.AwayFromZero) : 0m;
                    action.Params["allowedPercent"] = kpi.AllowedPercent.HasValue ? Math.Round(kpi.AllowedPercent.Value, 2, MidpointRounding.AwayFromZero) : 0m;
                    action.Params["targetPercent"] = Math.Round(kpi.TargetPercent, 2, MidpointRounding.AwayFromZero);
                }
                else if (action.Code == "ACT_CHECK_WEIGHING")
                {
                    action.Params["excessGram"] = kpi.ExcessGram;
                }
            }
        }

        private static Gold.KpiItem MapGoldKpi(ProductionGoldLossEvaluator.KpiResult r) => new Gold.KpiItem
        {
            WorkerType = r.WorkerType,
            Metal = r.Metal,
            SlipCount = r.SlipCount,
            WorkerCount = r.WorkerCount,
            ReceivedGram = r.ReceivedGram,
            RawLossGram = r.RawLossGram,
            AllowedGram = r.AllowedGram,
            LossPercent = r.LossPercent,
            AllowedPercent = r.AllowedPercent,
            TargetPercent = r.TargetPercent,
            ExcessGram = r.ExcessGram,
            ExcessMoney = r.ExcessMoney,
            NetMoney = r.NetMoney,
            OverCount = r.OverCount,
            WorkersOverCount = r.WorkersOverCount,
            CoverageJobs = r.CoverageJobs,
            CoverageTotalJobs = r.CoverageTotalJobs,
            CoveragePercent = r.CoveragePercent
        };

        private static Gold.SeriesItem MapGoldSeries(ProductionGoldLossEvaluator.SeriesResult r) => new Gold.SeriesItem
        {
            BucketEnd = r.BucketEnd,
            WorkerType = r.WorkerType,
            SlipCount = r.SlipCount,
            RawLossGram = r.RawLossGram,
            AllowedGram = r.AllowedGram,
            LossPercent = r.LossPercent,
            AllowedPercent = r.AllowedPercent
        };

        private static Gold.WorkerItem MapGoldWorker(ProductionGoldLossEvaluator.WorkerResult r) => new Gold.WorkerItem
        {
            WorkerType = r.WorkerType,
            WorkerCode = r.WorkerCode,
            WorkerName = r.WorkerName,
            SlipCount = r.SlipCount,
            ReceivedGram = r.ReceivedGram,
            LossPercent = r.LossPercent,
            AllowedPercent = r.AllowedPercent,
            TargetPercent = r.TargetPercent,
            ExcessGram = r.ExcessGram,
            ExcessMoney = r.ExcessMoney,
            NetMoney = r.NetMoney,
            OverBuckets = r.OverBuckets,
            QualifyingBuckets = r.QualifyingBuckets
        };

        // tang (50): 1 แถว = 1 slip เกิน allowance. setting (80): 1 แถว = 1 item เกิน allowance (ใช้ documentNo/
        // worker ของ slip แม่) — ดู comment บน GoldOverSlips.Item — reuse LossUnit normalization เดียวกับ Gold()
        // KPI ตรงๆ (รวม metal classification) กัน logic สองที่เพี้ยนไปจากกัน
        public async Task<DataSourceResult> GoldOverSlips(GoldOverSlips.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;
            var includeTang = request.WorkerTypes == null || request.WorkerTypes.Length == 0
                || request.WorkerTypes.Contains(ProductionGoldLossEvaluator.WorkerTypeTang);
            var includeSetting = request.WorkerTypes == null || request.WorkerTypes.Length == 0
                || request.WorkerTypes.Contains(ProductionGoldLossEvaluator.WorkerTypeSetting);
            var codeSet = request.WorkerCodes != null && request.WorkerCodes.Length > 0
                ? new HashSet<string>(request.WorkerCodes, StringComparer.OrdinalIgnoreCase)
                : null;

            var units = new List<ProductionGoldLossEvaluator.LossUnit>();

            if (includeTang)
            {
                var tangSlipsRaw = await _jewelryContext.TbtGoldLossTangSlip
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.RequestDateEnd != null && s.RequestDateEnd >= start && s.RequestDateEnd <= end)
                    .ToListAsync();

                var tangFiltered = codeSet != null ? tangSlipsRaw.Where(s => codeSet.Contains(s.WorkerCode)).ToList() : tangSlipsRaw;
                var tangItemsBySlip = await GetTangItemsBySlipAsync(tangFiltered.Select(s => s.Id).ToList());

                var tangRows = tangFiltered.Select(s => new ProductionGoldLossEvaluator.TangSlipRow
                {
                    SlipId = s.Id,
                    DocumentNo = s.DocumentNo,
                    WorkerCode = s.WorkerCode,
                    WorkerName = s.WorkerName,
                    RequestDateStart = s.RequestDateStart,
                    RequestDateEnd = s.RequestDateEnd,
                    ReturnedTotal = s.ReturnedTotal,
                    RawLoss = s.RawLoss,
                    AllowedLoss = s.AllowedLoss,
                    PricePerGram = s.PricePerGram,
                    TotalMoneyDiff = s.TotalMoneyDiff,
                    Items = tangItemsBySlip.TryGetValue(s.Id, out var its) ? its : new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>()
                }).ToList();

                units.AddRange(ProductionGoldLossEvaluator.NormalizeTang(tangRows).Where(u => u.Metal == metal));
            }

            if (includeSetting)
            {
                units.AddRange((await GetSettingUnitsAsync(start, end, codeSet)).Where(u => u.Metal == metal));
            }

            var items = units
                .Where(u => u.RawLossGram > u.AllowedGram)
                .Select(u =>
                {
                    var excessGram = u.RawLossGram - u.AllowedGram;
                    return new GoldOverSlips.Item
                    {
                        SlipId = u.SlipId,
                        DocumentNo = u.DocumentNo,
                        WorkerType = u.WorkerType,
                        WorkerCode = u.WorkerCode,
                        WorkerName = u.WorkerName,
                        RequestDateStart = u.BucketStartDate,
                        RequestDateEnd = u.BucketDate,
                        RawLossGram = Math.Round(u.RawLossGram, 4),
                        AllowedGram = Math.Round(u.AllowedGram, 4),
                        ExcessGram = Math.Round(excessGram, 4),
                        ExcessMoney = Math.Round(excessGram * (u.Price ?? 0), 2),
                        NetMoney = Math.Round(u.MoneyDiff, 2)
                    };
                })
                .ToList();

            IEnumerable<GoldOverSlips.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.ExcessGram);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        private class UncoveredJobRow
        {
            public int ProductionPlanId { get; set; }
            public int Status { get; set; }
            public string? Wo { get; set; }
            public int WoNumber { get; set; }
            public string? WoText { get; set; }
            public string? WorkerCode { get; set; }
            public string? HeaderWorkerName { get; set; }
            public string? Worker { get; set; }
            public string? WorkerSub { get; set; }
            public DateTime? RequestDate { get; set; }
            public decimal? GoldWeightSend { get; set; }
            public decimal? GoldWeightCheck { get; set; }
        }

        // ดึงงานที่ "ไม่คุ้มครอง" ของ 1 ประเภทช่าง — ใช้เงื่อนไขเดียวกับ ComputeGoldCoverageAsync ทุกจุด (รวม
        // BuildGoldCoveredExpr negate) เพื่อให้ KPI coverage กับตารางนี้ตรงกันเป๊ะ — 1 query ต่อ workerType
        // (ไม่ query ทีละแถว, ไม่ GroupBy.First)
        private async Task<List<UncoveredJobRow>> QueryUncoveredJobsAsync(
            int workerType, string metal, DateTime start, DateTime end, int olderThanDays, HashSet<string>? codeSet, DateTime now)
        {
            var query = _jewelryContext.TbtProductionPlanStatusDetail
                .AsNoTracking()
                .Include(d => d.Header)
                .ThenInclude(h => h.ProductionPlan)
                .Where(d => d.IsActive && d.Header.IsActive && d.Header.Status == workerType && d.GoldWeightCheck > 0
                    && d.RequestDate != null && d.RequestDate >= start && d.RequestDate <= end);

            query = FilterByMetal(query, metal);
            query = ExcludeTestPlans(query);

            if (olderThanDays > 0)
            {
                var cutoff = now.AddDays(-olderThanDays);
                query = query.Where(d => d.RequestDate != null && d.RequestDate <= cutoff);
            }

            if (codeSet != null)
            {
                query = query.Where(d => (d.Header.WorkerCode != null && codeSet.Contains(d.Header.WorkerCode))
                    || (d.Worker != null && codeSet.Contains(d.Worker))
                    || (d.WorkerSub != null && codeSet.Contains(d.WorkerSub)));
            }

            query = query.Where(NegateDetailExpr(BuildGoldCoveredExpr(workerType)));

            return await query
                .Select(d => new UncoveredJobRow
                {
                    ProductionPlanId = d.ProductionPlanId,
                    Status = d.Header.Status,
                    Wo = d.Header.ProductionPlan.Wo,
                    WoNumber = d.Header.ProductionPlan.WoNumber,
                    WoText = d.Header.ProductionPlan.WoText,
                    WorkerCode = d.Header.WorkerCode,
                    HeaderWorkerName = d.Header.WorkerName,
                    Worker = d.Worker,
                    WorkerSub = d.WorkerSub,
                    RequestDate = d.RequestDate,
                    GoldWeightSend = d.GoldWeightSend,
                    GoldWeightCheck = d.GoldWeightCheck
                })
                .ToListAsync();
        }

        // uncovered = status_detail (header active, status=50/80, gold_weight_check>0, ไม่ใช่แผนทดสอบ, โลหะตรง,
        // request_date ใน [start,end] เดียวกับ KPI coverage) ที่ "ไม่คุ้มครอง" ตามนิยามเดียวกับ
        // ComputeGoldCoverageAsync (stamp หรือ fallback match ด้วย production_plan_id/wo+wo_number+gold+วันที่ไทย)
        // — olderThanDays เป็นตัวกรองเสริมซ้อนช่วงอีกที — รันทีละประเภทช่าง (สูงสุด 2 query) แล้วรวมผล
        public async Task<DataSourceResult> GoldUncoveredJobs(GoldUncoveredJobs.Request request)
        {
            var now = DateTime.UtcNow;
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;

            var types = new List<int>();
            if (request.WorkerTypes == null || request.WorkerTypes.Length == 0)
            {
                types.Add(ProductionGoldLossEvaluator.WorkerTypeTang);
                types.Add(ProductionGoldLossEvaluator.WorkerTypeSetting);
            }
            else
            {
                types.AddRange(request.WorkerTypes.Where(t => t == ProductionGoldLossEvaluator.WorkerTypeTang || t == ProductionGoldLossEvaluator.WorkerTypeSetting));
            }

            var codeSet = request.WorkerCodes != null && request.WorkerCodes.Length > 0
                ? new HashSet<string>(request.WorkerCodes, StringComparer.OrdinalIgnoreCase)
                : null;

            var rows = new List<UncoveredJobRow>();
            foreach (var wt in types)
            {
                rows.AddRange(await QueryUncoveredJobsAsync(wt, metal, start, end, request.OlderThanDays, codeSet, now));
            }

            var items = rows.Select(r =>
            {
                var send = r.GoldWeightSend ?? 0;
                var check = r.GoldWeightCheck ?? 0;
                var jobDate = r.RequestDate ?? default;
                var resolvedCode = !string.IsNullOrWhiteSpace(r.WorkerCode) ? r.WorkerCode
                    : !string.IsNullOrWhiteSpace(r.Worker) ? r.Worker
                    : r.WorkerSub;

                return new GoldUncoveredJobs.Item
                {
                    PlanId = r.ProductionPlanId,
                    Wo = r.Wo ?? string.Empty,
                    WoNumber = r.WoNumber,
                    WoText = r.WoText ?? string.Empty,
                    DeptKey = ProductionPlanDepartments.DepartmentKeyOf(r.Status),
                    WorkerCode = resolvedCode,
                    WorkerName = r.HeaderWorkerName,
                    JobDate = jobDate,
                    SendGram = Math.Round(send, 4),
                    CheckGram = Math.Round(check, 4),
                    DiffGram = Math.Round(send - check, 4),
                    DaysSince = (int)(now - jobDate).TotalDays
                };
            }).ToList();

            IEnumerable<GoldUncoveredJobs.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysSince);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // worker_type ที่ valid ต่อ scope — SLIP = worker_type ของใบ gold loss, STAGE = สถานะทำงานของแผนก
        private static readonly int[] SlipWorkerTypes = { ProductionGoldLossEvaluator.WorkerTypeTang, ProductionGoldLossEvaluator.WorkerTypeSetting };
        private static readonly int[] StageWorkerTypes = { 60, 80, 90 };

        public async Task<List<GoldLossTarget.Item>> GetGoldLossTargets()
        {
            var current = await _goldLossTargetService.GetCurrentTargetsAsync();
            var metals = new[] { ProductionGoldLossEvaluator.MetalGold, ProductionGoldLossEvaluator.MetalSilver };

            var result = new List<GoldLossTarget.Item>();

            foreach (var wt in SlipWorkerTypes)
            {
                foreach (var metal in metals)
                {
                    result.Add(current.TryGetValue(("SLIP", wt, metal), out var row)
                        ? new GoldLossTarget.Item { Scope = "SLIP", WorkerType = wt, Metal = metal, TargetPercent = row.TargetPercent, EffectiveFrom = row.EffectiveFrom, CreateBy = row.CreateBy, Remark = row.Remark }
                        : new GoldLossTarget.Item
                        {
                            Scope = "SLIP",
                            WorkerType = wt,
                            Metal = metal,
                            TargetPercent = DefaultSlipGoldLossTargetPercent(wt, metal),
                            EffectiveFrom = default,
                            CreateBy = "system",
                            Remark = "ไม่มีข้อมูลในตาราง ใช้ค่า default"
                        });
                }
            }

            foreach (var wt in StageWorkerTypes)
            {
                foreach (var metal in metals)
                {
                    result.Add(current.TryGetValue(("STAGE", wt, metal), out var row)
                        ? new GoldLossTarget.Item { Scope = "STAGE", WorkerType = wt, Metal = metal, TargetPercent = row.TargetPercent, EffectiveFrom = row.EffectiveFrom, CreateBy = row.CreateBy, Remark = row.Remark }
                        : new GoldLossTarget.Item
                        {
                            Scope = "STAGE",
                            WorkerType = wt,
                            Metal = metal,
                            TargetPercent = DefaultStageGoldLossTargetPercent(wt, metal),
                            EffectiveFrom = default,
                            CreateBy = "system",
                            Remark = "ไม่มีข้อมูลในตาราง ใช้ค่า default"
                        });
                }
            }

            return result;
        }

        public async Task<List<GoldLossTarget.Item>> GetGoldLossTargetHistory(string scope, int workerType, string metal)
        {
            var normalizedScope = scope == "STAGE" ? "STAGE" : "SLIP";
            var normalizedMetal = IsValidMetal(metal) ? metal : ProductionGoldLossEvaluator.MetalGold;
            var rows = await _goldLossTargetService.GetHistoryAsync(normalizedScope, workerType, normalizedMetal);
            return rows.Select(r => new GoldLossTarget.Item
            {
                Scope = r.Scope,
                WorkerType = r.WorkerType,
                Metal = r.Metal,
                TargetPercent = r.TargetPercent,
                EffectiveFrom = r.EffectiveFrom,
                CreateBy = r.CreateBy,
                Remark = r.Remark
            }).ToList();
        }

        public async Task SaveGoldLossTargets(SaveGoldLossTargets.Request request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException("ต้องระบุอย่างน้อย 1 รายการ");
            }

            foreach (var item in request.Items)
            {
                var scope = string.IsNullOrWhiteSpace(item.Scope) ? "SLIP" : item.Scope;
                if (scope != "SLIP" && scope != "STAGE")
                {
                    throw new ArgumentException($"ไม่รู้จัก scope '{item.Scope}'");
                }

                var validTypes = scope == "STAGE" ? StageWorkerTypes : SlipWorkerTypes;
                if (!validTypes.Contains(item.WorkerType))
                {
                    throw new ArgumentException($"ไม่รู้จักประเภทช่าง/แผนก '{item.WorkerType}' สำหรับ scope '{scope}'");
                }

                if (!IsValidMetal(item.Metal))
                {
                    throw new ArgumentException($"ไม่รู้จักโลหะ '{item.Metal}'");
                }

                if (item.TargetPercent < 0 || item.TargetPercent > 100)
                {
                    throw new ArgumentException($"เป้าหมาย % ของ '{scope}/{item.WorkerType}' ({item.Metal}) ต้องอยู่ระหว่าง 0-100");
                }
            }

            if (string.IsNullOrWhiteSpace(request.Remark))
            {
                throw new ArgumentException("ต้องระบุหมายเหตุ");
            }

            var items = request.Items
                .Select(i => (string.IsNullOrWhiteSpace(i.Scope) ? "SLIP" : i.Scope, i.WorkerType, i.Metal, i.TargetPercent))
                .ToList();
            await _goldLossTargetService.SaveAsync(items, request.Remark, CurrentUsername);
        }

        // ---- Capacity (กำลังการผลิต) ----

        public async Task<Capacity.Response> Capacity(Capacity.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;

            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var doneDates = await _deliveryDataProvider.GetDoneDatesAsync();
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIds = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));

            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);
            var activeWipByDept = ComputeActiveWipByDept(openPlans, stalePlanIds);
            var nonStaleOpenPlans = openPlans.Where(p => !stalePlanIds.Contains(p.Id)).ToList();

            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();
            var standardByDept = ProductionPlanDepartments.Departments.ToDictionary(
                d => d.Key,
                d => savedStandards.TryGetValue(d.Key, out var s) ? s.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays);

            // บังคับ bucket เป็น month เสมอ ("Monthly buckets by Thai date" — request.Bucket ไม่ได้ใช้เลือก week)
            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, "month").Select(b => b.End).ToList();

            // ---- จุดเริ่ม/ความยาว (วัน) ของแต่ละ bucket — ใช้ 3 เรื่อง: (1) peakMonth label เอาจาก "วันที่เริ่ม"
            // ไม่ใช่ "วันที่จบ" ของ bucket (ชื่อเดือนของวันที่จบมักเป็นเดือนถัดไปเสมอ ไม่ตรงกับข้อมูลในนั้น)
            // (2) overloadMonths ไม่นับ bucket ที่สั้นกว่า 15 วัน (3) ไม่ใช้ bucketEnds.Count เป็นตัวหารค่าเฉลี่ย/เดือน
            // อีกต่อไป (bucket เดือนสุดท้ายอาจสั้นกว่า 1 เดือนมาก ถ้า end ตรงกับวันที่ 1 พอดี ทำให้เฉลี่ยเพี้ยน) ----
            var bucketStarts = new List<DateTime>();
            var bucketDurationDays = new List<double>();
            {
                var prevBoundary = start;
                foreach (var be in bucketEnds)
                {
                    bucketStarts.Add(prevBoundary);
                    bucketDurationDays.Add((be - prevBoundary).TotalDays);
                    prevBoundary = be;
                }
            }

            var rangeDaysForMonths = Math.Max((end - start).TotalDays, 1);
            var monthsForRates = Math.Max(rangeDaysForMonths / 30.44, 0.1);
            var monthsInRangeRounded = Math.Max((int)Math.Round(rangeDaysForMonths / 30.44, MidpointRounding.AwayFromZero), 1);

            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(
                visits, ProductionPlanDepartments.Departments, standardByDept, activeWipByDept, start, end);

            var first95Dates = ProductionCapacityEvaluator.ComputeFirst95Dates(trendData.Headers);
            var meltedDates = ProductionCapacityEvaluator.ComputeMeltedDates(trendData.Plans, trendData.Headers);
            var wipTimes = ProductionPlanTrendEvaluator.ComputeWipCountsAtTimes(trendData.Plans, trendData.Headers, ProductionPlanDepartments.DepartmentKeyOf, bucketEnds);
            var seriesRaw = ProductionCapacityEvaluator.ComputeSeries(trendData.Plans, first95Dates, doneDates, meltedDates, wipTimes, start, bucketEnds, now);

            var totalInflow = seriesRaw.Sum(s => s.Inflow);
            var totalInflowPieces = seriesRaw.Sum(s => s.InflowPieces);
            var totalOutput = seriesRaw.Sum(s => s.Output);
            var totalCompleted = seriesRaw.Sum(s => s.Completed);

            var inflowPerMonth = (decimal)totalInflow / (decimal)monthsForRates;
            var inflowPiecesPerMonth = (decimal)totalInflowPieces / (decimal)monthsForRates;
            var outputPerMonth = (decimal)totalOutput / (decimal)monthsForRates;
            var completedPerMonth = (decimal)totalCompleted / (decimal)monthsForRates;
            // netPerMonth = inflow - output (ไม่ใช่ - completed) ให้สอดคล้องกับ backlogMonths ที่สเปกระบุให้หาร
            // ด้วย outputPerMonth ตรงๆ — มองสายการผลิตจบที่บัตรต้นทุน ส่วนช้า 95→100 แยกติดตามที่ costCardToDone
            var netPerMonth = inflowPerMonth - outputPerMonth;

            // = Σ activeWipByDept (แผนกที่ไม่มี deptKey เช่น CVD 84/85 ไม่ถูกนับ — ตรงกับนิยาม "status ไม่ใช่
            // 100/500/84/85" ที่ใช้ตลอดทั้งหน้านี้) — ไม่ใช้ openPlans.Count ตรงๆ อีกต่อไป (นับ 84/85 ปนเข้ามาทำให้
            // ตัวเลขรวมไม่ตรงกับผลรวมรายแผนก)
            var activeWip = activeWipByDept.Values.Sum();
            var staleWip = stalePlanIds.Count;
            var backlogMonths = outputPerMonth > 0 ? (decimal?)Math.Round(activeWip / outputPerMonth, 1) : null;

            // ไม่นับ bucket สั้นกว่า 15 วัน (bucket ท้ายๆ ที่ถูกตัดสั้นเพราะ end ตรงกับวันที่ 1 พอดี ไม่ควรถูกเข้าใจ
            // ผิดว่าเป็น "เดือนที่ inflow เกิน output" ทั้งที่จริงมีข้อมูลแค่ไม่กี่วัน)
            var overloadMonths = seriesRaw
                .Where((s, i) => bucketDurationDays[i] >= 15 && s.Inflow > s.Output)
                .Count();

            var workerNames = await _workerLookupService.GetWorkerNamesAsync();
            var activityRowsRaw = await GetWorkerActivityRowsAsync(start, end);
            // ไม่นับรหัส placeholder ("รอจ่าย...") หรือไม่มีรหัสช่างเลย เป็น "ช่าง" ในการนับ/หา median — รหัสพวกนี้
            // คือคิวรองาน ไม่ใช่คนจริง
            var activityRows = activityRowsRaw
                .Where(r => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(r.WorkerCode, workerNames))
                .ToList();
            var workersByDept = ProductionCapacityEvaluator.ComputeWorkersByDept(activityRows, ProductionPlanDepartments.Departments, start, bucketEnds);

            var departmentsOut = new List<Capacity.DepartmentItem>();
            foreach (var dept in ProductionPlanDepartments.Departments)
            {
                var capRow = capacity.Departments.First(d => d.Key == dept.Key);
                var waitStatus = dept.StatusIds.Length >= 2 ? dept.StatusIds[0] : (int?)null;
                var workStatus = dept.StatusIds.Length >= 2 ? dept.StatusIds[1] : dept.StatusIds[0];
                // waitingNow/workingNow/activeWip (capRow.ActiveWip) ต้องตัด stale ออกเหมือนกันหมด (นิยามเดียวกับ
                // kpi.activeWip) — ก่อนหน้านี้นับจาก trendData.Plans ตรงๆ ซึ่งรวม stale ด้วย ทำให้ตัวเลขพองเกินจริง
                var waitingNow = waitStatus.HasValue ? nonStaleOpenPlans.Count(p => p.Status == waitStatus.Value) : 0;
                var workingNow = nonStaleOpenPlans.Count(p => p.Status == workStatus);
                var exitsPerMonth = (decimal)capRow.ExitedCount / (decimal)monthsForRates;
                var workersInfo = workersByDept.TryGetValue(dept.Key, out var wi) ? wi : new ProductionCapacityEvaluator.WorkersResult();
                decimal? plansPerWorker = workersInfo.WorkersMedian.HasValue && workersInfo.WorkersMedian.Value > 0
                    ? Math.Round(exitsPerMonth / (decimal)workersInfo.WorkersMedian.Value, 1)
                    : (decimal?)null;
                var deptVisitsForExits = visits.Where(v => v.DeptKey == dept.Key).ToList();
                var exitsSeries = ProductionCapacityEvaluator.ComputeExitsSeries(deptVisitsForExits, start, bucketEnds);

                departmentsOut.Add(new Capacity.DepartmentItem
                {
                    Key = dept.Key,
                    ExitsPerMonth = Math.Round(exitsPerMonth, 1),
                    ExitsSeries = exitsSeries.Select(s => new Capacity.ExitsSeriesPoint { BucketEnd = s.BucketEnd, Exits = s.Exits }).ToList(),
                    WorkersMedian = workersInfo.WorkersMedian,
                    WorkersSeries = workersInfo.Series.Select(s => new Capacity.WorkersSeriesPoint { BucketEnd = s.BucketEnd, Workers = s.Workers }).ToList(),
                    PlansPerWorker = plansPerWorker,
                    WaitingNow = waitingNow,
                    WorkingNow = workingNow,
                    ActiveWip = capRow.ActiveWip,
                    QueueDays = capRow.QueueDaysCurrent,
                    IsBottleneck = false
                });
            }

            var bottleneckDepts = departmentsOut
                .Where(d => d.QueueDays.HasValue)
                .OrderByDescending(d => d.QueueDays!.Value)
                .Take(ProductionInsightThresholds.CapQueueBottleneckTopCount)
                .Select(d => d.Key)
                .ToList();
            foreach (var d in departmentsOut)
            {
                d.IsBottleneck = bottleneckDepts.Contains(d.Key);
            }

            var cc = ProductionCapacityEvaluator.ComputeCostCardToDone(
                first95Dates, doneDates, trendData.Plans, start, bucketEnds, now, ProductionInsightThresholds.CapCostCardPendingOver30dDays, stalePlanIds);

            var problems = new List<Wip.Finding>();
            var forecasts = new List<Wip.Finding>();

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateCapBacklogMonths(backlogMonths, activeWip, outputPerMonth));

            var bottleneckRows = departmentsOut.Where(d => d.IsBottleneck).OrderByDescending(d => d.QueueDays ?? 0).ToList();
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateCapQueueBottleneck(
                bottleneckRows.Select(d => (d.Key, d.QueueDays ?? 0, d.WaitingNow)).ToList()));

            // peakMonth label ต้องมาจาก "วันเริ่ม" ของ bucket ไม่ใช่วันจบ — bucket (1พ.ค.,1มิ.ย.] เก็บข้อมูลของ
            // เดือนพฤษภาคม แต่วันจบ (1มิ.ย.) format เป็น "yyyy-MM" จะได้ "06" (มิถุนายน) ผิดเดือนเสมอ
            var peakIndex = -1;
            var peakInflowValue = -1;
            for (var i = 0; i < seriesRaw.Count; i++)
            {
                if (seriesRaw[i].Inflow > peakInflowValue)
                {
                    peakInflowValue = seriesRaw[i].Inflow;
                    peakIndex = i;
                }
            }
            var peak = peakIndex >= 0 ? seriesRaw[peakIndex] : null;
            var peakMonthLabel = peakIndex >= 0 ? bucketStarts[peakIndex].AddHours(7).ToString("yyyy-MM") : null;

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateCapInflowOverOutput(overloadMonths, monthsInRangeRounded, peakMonthLabel, peak?.Inflow ?? 0, outputPerMonth));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateCapCostCardSlow(cc.MedianDays, cc.P90Days, cc.PendingNow, cc.PendingActive, cc.PendingOver30d));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcBacklogProjected(activeWip, netPerMonth));

            var topBottleneck = bottleneckRows.FirstOrDefault();
            var extraQueueDays = 0.0;
            if (topBottleneck != null && peak != null && outputPerMonth > 0)
            {
                var excessPlans = (double)(peak.Inflow - outputPerMonth);
                var topBottleneckCapRow = capacity.Departments.First(d => d.Key == topBottleneck.Key);
                if (excessPlans > 0 && topBottleneckCapRow.ExitedPerDay > 0)
                {
                    extraQueueDays = excessPlans / topBottleneckCapRow.ExitedPerDay;
                }
            }
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcPeakRisk(peakMonthLabel, peak?.Inflow ?? 0, outputPerMonth, extraQueueDays));

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            EnrichCapacityActionParams(actions, departmentsOut, cc, staleWip, outputPerMonth);

            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Capacity.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                Kpi = new Capacity.KpiData
                {
                    InflowPerMonth = Math.Round(inflowPerMonth, 1),
                    InflowPiecesPerMonth = Math.Round(inflowPiecesPerMonth, 1),
                    OutputPerMonth = Math.Round(outputPerMonth, 1),
                    CompletedPerMonth = Math.Round(completedPerMonth, 1),
                    NetPerMonth = Math.Round(netPerMonth, 1),
                    ActiveWip = activeWip,
                    BacklogMonths = backlogMonths,
                    StaleWip = staleWip,
                    BottleneckDepts = bottleneckDepts,
                    OverloadMonths = overloadMonths,
                    MonthsInRange = monthsInRangeRounded
                },
                Series = seriesRaw.Select(s => new Capacity.SeriesItem
                {
                    BucketEnd = s.BucketEnd,
                    Inflow = s.Inflow,
                    InflowPieces = s.InflowPieces,
                    Output = s.Output,
                    Completed = s.Completed,
                    Melted = s.Melted,
                    ActiveWipEnd = s.ActiveWipEnd
                }).ToList(),
                Departments = departmentsOut,
                CostCardToDone = new Capacity.CostCardToDoneData
                {
                    MedianDays = cc.MedianDays,
                    P90Days = cc.P90Days,
                    PendingNow = cc.PendingNow,
                    PendingActive = cc.PendingActive,
                    PendingStale = cc.PendingStale,
                    PendingOver30d = cc.PendingOver30d,
                    Series = cc.Series.Select(s => new Capacity.CostCardSeriesPoint
                    {
                        BucketEnd = s.BucketEnd,
                        Count = s.Count,
                        MedianDays = s.MedianDays,
                        P90Days = s.P90Days
                    }).ToList()
                }
            };
        }

        // activeWip ต่อแผนก = แผนเปิดที่ไม่ stale (นิยามเดียวกับ WIP_STALE) นับตาม plan.status ปัจจุบัน รวมทั้งสถานะ
        // รอ+ทำงานของแผนกนั้น (เช่น trim = 49+50) — ใช้เป็น input ของ BuildCapacity (queue-days bottleneck)
        private static Dictionary<string, int> ComputeActiveWipByDept(IReadOnlyList<OpenPlanRow> openPlans, HashSet<int> stalePlanIds)
        {
            var result = new Dictionary<string, int>();
            foreach (var plan in openPlans)
            {
                if (stalePlanIds.Contains(plan.Id)) continue;
                var deptKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status);
                if (deptKey == null) continue;
                result.TryGetValue(deptKey, out var c);
                result[deptKey] = c + 1;
            }
            return result;
        }

        // ดึง (deptKey, header.create_date, header.worker_code) ของทุก header ที่ active ในช่วง — ใช้
        // header.worker_code (ไม่ใช่ status_detail.worker/worker_sub) เป็นแหล่งข้อมูลช่าง: query เดียวเบากว่า
        // พอสำหรับภาพรวมรายเดือน — ตัวเลขอาจคลาดเคลื่อนเล็กน้อยจาก prod ที่ coordinator ตรวจด้วย status_detail.worker
        // (เอกสารไว้ใน report)
        // ช่างจริงต่อ header มาจาก status_detail.worker/worker_sub (ของ detail ที่ active ในหัวนั้น) ไม่ใช่
        // header.worker_code เฉยๆ (undercounts — header.worker_code มีแค่ "ช่างหลัก" คนเดียว ขณะที่งานหนึ่งหัวอาจ
        // มีหลายช่างทำจริงที่ detail level — ตรวจแล้วบน prod เช่นแผนกฝังมีช่าง distinct 10-12 คน/เดือน ไม่ใช่
        // ไม่กี่คนแบบนับจาก worker_code) — LEFT JOIN header-detail เป็น query เดียว (set-based) แล้ว group/fallback
        // ในหน่วยความจำ: header ไหนไม่มี worker ระดับ detail เลย (ไม่มี record หรือ worker/worker_sub ว่างหมด)
        // fallback ไปใช้ header.worker_code แทน — header ที่มี detail worker จะได้ 1 แถวต่อช่าง distinct 1 คน
        private async Task<List<(string DeptKey, DateTime CreateDate, string? WorkerCode)>> GetWorkerActivityRowsAsync(DateTime start, DateTime end)
        {
            var raw = await (
                from h in _jewelryContext.TbtProductionPlanStatusHeader
                where h.IsActive && h.CreateDate >= start && h.CreateDate <= end
                join d in _jewelryContext.TbtProductionPlanStatusDetail.Where(x => x.IsActive)
                    on h.Id equals d.HeaderId into details
                from d in details.DefaultIfEmpty()
                select new
                {
                    h.Id,
                    h.Status,
                    h.CreateDate,
                    h.WorkerCode,
                    DetailWorker = d.Worker,
                    DetailWorkerSub = d.WorkerSub
                })
                .AsNoTracking()
                .ToListAsync();

            var result = new List<(string, DateTime, string?)>();

            foreach (var headerGroup in raw.GroupBy(r => r.Id))
            {
                var first = headerGroup.First();
                var deptKey = ProductionPlanDepartments.DepartmentKeyOf(first.Status);
                if (deptKey == null) continue;

                var detailWorkers = headerGroup
                    .SelectMany(r => new[] { r.DetailWorker, r.DetailWorkerSub })
                    .Where(w => !string.IsNullOrWhiteSpace(w))
                    .Select(w => w!.Trim())
                    .Distinct()
                    .ToList();

                if (detailWorkers.Count > 0)
                {
                    foreach (var w in detailWorkers)
                    {
                        result.Add((deptKey, first.CreateDate, w));
                    }
                }
                else
                {
                    result.Add((deptKey, first.CreateDate, first.WorkerCode));
                }
            }

            return result;
        }

        // ACT_ADD_WORKER/ACT_SPEED_COSTCARD/ACT_CLEAN_STALE/ACT_SMOOTH_INFLOW: เติมทับ param จาก kpi/departments
        // ตรงๆ เสมอ (ไม่พึ่ง finding param ที่อาจไม่ fire) — บทเรียนเดียวกับ Gold.ACT_REVIEW_ALLOWANCE
        private static void EnrichCapacityActionParams(
            List<Wip.ActionItem> actions, List<Capacity.DepartmentItem> departments,
            ProductionCapacityEvaluator.CostCardToDoneStats cc, int staleWip, decimal outputPerMonth)
        {
            foreach (var action in actions)
            {
                switch (action.Code)
                {
                    case "ACT_ADD_WORKER":
                    {
                        var top = departments
                            .Where(d => d.Key != "costCard" && d.QueueDays.HasValue)
                            .OrderByDescending(d => d.QueueDays!.Value)
                            .FirstOrDefault();
                        if (top == null) break;

                        var workersNow = top.WorkersMedian ?? 0;
                        var queueDaysNow = top.QueueDays ?? 0;
                        var queueDaysPlusOne = workersNow > 0 ? queueDaysNow * workersNow / (workersNow + 1) : queueDaysNow;

                        action.Params["deptKey"] = top.Key;
                        action.Params["workersNow"] = Math.Round(workersNow, 1);
                        action.Params["queueDaysNow"] = Math.Round(queueDaysNow, 1);
                        action.Params["queueDaysPlusOne"] = Math.Round(queueDaysPlusOne, 1);
                        break;
                    }
                    case "ACT_SPEED_COSTCARD":
                        // เร่งรัดเฉพาะของที่ยังเคลื่อนไหวจริง (ไม่ stale) — pendingNow (รวม stale) แค่ให้บริบท
                        action.Params["pendingActive"] = cc.PendingActive;
                        action.Params["pendingNow"] = cc.PendingNow;
                        action.Params["medianDays"] = cc.MedianDays.HasValue ? Math.Round(cc.MedianDays.Value, 1) : 0.0;
                        break;
                    case "ACT_CLEAN_STALE":
                        action.Params["staleWip"] = staleWip;
                        break;
                    case "ACT_SMOOTH_INFLOW":
                        action.Params["outputPerMonth"] = Math.Round(outputPerMonth, 1);
                        break;
                }
            }
        }

        // ไม่กรอง stale ออก (ตามสั่ง "keep all") — ไม่เติม LastUpdateBy/LastAction/Workers (ดู comment เดียวกับ
        // DeliveryLatePlans.Item) — costCardDate ใช้ "เข้า 95 ครั้งแรก" ให้ตรงกับนิยาม output ใหม่ (ต่างจาก
        // StuckAfterCostCardPlans เดิมของ Delivery ที่ใช้ "ล่าสุด" MAX — ดู report)
        public async Task<DataSourceResult> CostCardPendingPlans(CostCardPendingPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var doneDates = await _deliveryDataProvider.GetDoneDatesAsync();
            var statusNames = await _wipHelper.GetStatusNamesAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();

            var first95Dates = ProductionCapacityEvaluator.ComputeFirst95Dates(trendData.Headers);
            var excluded = new HashSet<int> { ProductionPlanStatusConst.Completed, ProductionPlanStatusConst.Melted, ProductionPlanStatusConst.WaitCVD, ProductionPlanStatusConst.CVD };

            var items = new List<CostCardPendingPlans.Item>();
            foreach (var plan in trendData.Plans)
            {
                if (!first95Dates.TryGetValue(plan.Id, out var costCardDate)) continue;
                if (doneDates.ContainsKey(plan.Id)) continue;
                if (excluded.Contains(plan.Status)) continue;

                var lastMove = lastMoveDates.TryGetValue(plan.Id, out var lm) ? lm : plan.CreateDate;

                items.Add(new CostCardPendingPlans.Item
                {
                    PlanId = plan.Id,
                    Wo = plan.Wo,
                    WoNumber = plan.WoNumber,
                    WoText = plan.WoText,
                    Mold = plan.Mold,
                    ProductNumber = plan.ProductNumber,
                    ProductName = plan.ProductName,
                    ProductQty = plan.ProductQty,
                    StatusId = plan.Status,
                    StatusName = statusNames.TryGetValue(plan.Status, out var name) ? name : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(plan.Status),
                    CreateDate = plan.CreateDate,
                    LastMoveDate = lastMove,
                    DaysSinceMove = (int)(now - lastMove).TotalDays,
                    CostCardDate = costCardDate,
                    DaysSinceCostCard = Math.Round((now - costCardDate).TotalDays, 1)
                });
            }

            IEnumerable<CostCardPendingPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysSinceCostCard);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // ---- Loss ตามใบงานรายแผนก (จ่าย-รับ) ----

        private class GoldStageSummary
        {
            public List<ProductionGoldStageEvaluator.StageRow> AllRows { get; set; } = new List<ProductionGoldStageEvaluator.StageRow>();
            public Dictionary<string, ProductionGoldStageEvaluator.DeptStats> DeptStatsByDept { get; set; } = new Dictionary<string, ProductionGoldStageEvaluator.DeptStats>();
            public Dictionary<string, List<ProductionGoldStageEvaluator.SeriesPoint>> SeriesByDept { get; set; } = new Dictionary<string, List<ProductionGoldStageEvaluator.SeriesPoint>>();
            // "โทษได้" (มีช่างจริงถือครอง) / "หาตัวไม่ได้" (placeholder/ไม่มีช่าง) — แยกกันตามที่สั่ง
            public Dictionary<string, List<ProductionGoldStageEvaluator.OutlierRow>> OutliersAttributedByDept { get; set; } = new Dictionary<string, List<ProductionGoldStageEvaluator.OutlierRow>>();
            public Dictionary<string, List<ProductionGoldStageEvaluator.OutlierRow>> OutliersUnassignedByDept { get; set; } = new Dictionary<string, List<ProductionGoldStageEvaluator.OutlierRow>>();
            public Dictionary<string, decimal?> TargetPercentByDept { get; set; } = new Dictionary<string, decimal?>();

            // 'saved' | 'draft' — มีความหมายเฉพาะแผนกที่มี targetPercent ไม่ null
            public Dictionary<string, string> TargetSourceByDept { get; set; } = new Dictionary<string, string>();
            public Dictionary<string, decimal?> SlipLossPercentByDept { get; set; } = new Dictionary<string, decimal?>();
        }

        // scope=STAGE worker_type (สถานะทำงานของแผนก ไม่ใช่ worker_type ของใบ gold loss) ต่อแผนกที่มีเป้าหมาย —
        // trim/gemSort ไม่มี (ไม่อยู่ใน map นี้ -> TargetPercentByDept เป็น null)
        private static readonly Dictionary<string, int> StageDeptTargetWorkerTypeMap = new Dictionary<string, int>
        {
            [ProductionGoldStageEvaluator.DeptRawPolish] = 60,
            [ProductionGoldStageEvaluator.DeptSetting] = 80,
            [ProductionGoldStageEvaluator.DeptPlating] = 90
        };

        // แผนกที่มีใบ gold loss (slip) คู่กันสำหรับเทียบ slipLossPercent — เฉพาะ trim(50)/setting(80)
        private static readonly Dictionary<string, int> StageDeptSlipWorkerTypeMap = new Dictionary<string, int>
        {
            [ProductionGoldStageEvaluator.DeptTrim] = ProductionGoldLossEvaluator.WorkerTypeTang,
            [ProductionGoldStageEvaluator.DeptSetting] = ProductionGoldLossEvaluator.WorkerTypeSetting
        };

        // query เดียว (LEFT join header-detail ผ่าน HeaderId) — ตรง rule เดียวกับ Production/Plan
        // GetGoldLossByStageReport (header active, detail active, gold_weight_send>0) + เพิ่ม "gold ไม่ว่าง" และ
        // กรองโลหะ (ตาม request) ที่ของเดิมไม่มี — ขอบเขตวันที่ = header.create_date (Thai ผ่าน start/end ที่
        // caller แปลงมาแล้ว)
        private async Task<List<ProductionGoldStageEvaluator.StageRow>> GetGoldStageRowsAsync(DateTime start, DateTime end, string metal)
        {
            var statuses = new[] { 50, 60, 70, 80, 90 };

            var raw = await (
                from detail in _jewelryContext.TbtProductionPlanStatusDetail
                join header in _jewelryContext.TbtProductionPlanStatusHeader on detail.HeaderId equals header.Id
                where header.IsActive && detail.IsActive && detail.GoldWeightSend > 0
                    && detail.Gold != null && detail.Gold != ""
                    && statuses.Contains(header.Status)
                    && header.CreateDate >= start && header.CreateDate <= end
                select new
                {
                    header.Id,
                    header.Status,
                    header.CreateDate,
                    header.WorkerCode,
                    detail.Worker,
                    detail.WorkerSub,
                    detail.RequestDate,
                    detail.Gold,
                    GoldWeightSend = detail.GoldWeightSend ?? 0,
                    detail.GoldWeightCheck,
                    detail.ProductionPlanId,
                    header.ProductionPlan.Wo,
                    header.ProductionPlan.WoNumber,
                    header.ProductionPlan.WoText,
                    header.ProductionPlan.ProductName,
                    header.ProductionPlan.ProductNumber,
                    header.ProductionPlan.CustomerNumber
                })
                .AsNoTracking()
                .ToListAsync();

            return raw
                .Where(r => ProductionGoldLossEvaluator.ClassifyMetal(r.Gold) == metal)
                .Select(r => new ProductionGoldStageEvaluator.StageRow
                {
                    DeptKey = ProductionPlanDepartments.DepartmentKeyOf(r.Status) ?? string.Empty,
                    HeaderId = r.Id,
                    HeaderCreateDate = r.CreateDate,
                    DetailRequestDate = r.RequestDate,
                    WorkerCode = !string.IsNullOrWhiteSpace(r.Worker) ? r.Worker : (!string.IsNullOrWhiteSpace(r.WorkerSub) ? r.WorkerSub : r.WorkerCode),
                    SendGram = r.GoldWeightSend,
                    CheckGram = r.GoldWeightCheck,
                    IsReturned = r.GoldWeightCheck.HasValue,
                    PlanId = r.ProductionPlanId,
                    Wo = r.Wo,
                    WoNumber = r.WoNumber,
                    WoText = r.WoText,
                    ProductName = r.ProductName,
                    ProductNumber = r.ProductNumber,
                    CustomerNumber = r.CustomerNumber
                })
                .ToList();
        }

        // % loss จากฝั่งใบ gold loss (scope=SLIP) ของ workerType/metal/ช่วงเดียวกัน — ไว้เทียบกับ diffPercent ฝั่ง
        // รายแผนก (trim=50, setting=80 เท่านั้นที่มีใบคู่กัน) — สูตรเดียวกับ ProductionGoldLossEvaluator.ComputeKpi.LossPercent
        private async Task<decimal?> GetSlipLossPercentAsync(int workerType, string metal, DateTime start, DateTime end)
        {
            List<ProductionGoldLossEvaluator.LossUnit> units;

            if (workerType == ProductionGoldLossEvaluator.WorkerTypeTang)
            {
                var tangSlipsRaw = await _jewelryContext.TbtGoldLossTangSlip
                    .AsNoTracking()
                    .Where(s => s.IsActive && s.RequestDateEnd != null && s.RequestDateEnd >= start && s.RequestDateEnd <= end)
                    .ToListAsync();
                var tangItemsBySlip = await GetTangItemsBySlipAsync(tangSlipsRaw.Select(s => s.Id).ToList());
                var tangRows = tangSlipsRaw.Select(s => new ProductionGoldLossEvaluator.TangSlipRow
                {
                    SlipId = s.Id,
                    DocumentNo = s.DocumentNo,
                    WorkerCode = s.WorkerCode,
                    WorkerName = s.WorkerName,
                    RequestDateStart = s.RequestDateStart,
                    RequestDateEnd = s.RequestDateEnd,
                    ReturnedTotal = s.ReturnedTotal,
                    RawLoss = s.RawLoss,
                    AllowedLoss = s.AllowedLoss,
                    PricePerGram = s.PricePerGram,
                    TotalMoneyDiff = s.TotalMoneyDiff,
                    Items = tangItemsBySlip.TryGetValue(s.Id, out var its) ? its : new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>()
                }).ToList();
                units = ProductionGoldLossEvaluator.NormalizeTang(tangRows).Where(u => u.Metal == metal).ToList();
            }
            else
            {
                units = (await GetSettingUnitsAsync(start, end, null)).Where(u => u.Metal == metal).ToList();
            }

            if (units.Count == 0) return null;

            var receivedGram = units.Sum(u => u.ReceivedGram);
            var rawLossGram = units.Sum(u => u.RawLossGram);
            return receivedGram > 0 ? Math.Round(rawLossGram / receivedGram * 100, 2) : (decimal?)null;
        }

        // stageDraftByWorkerType: what-if override ของ scope=STAGE เฉพาะ metal นี้ (key = worker_type แผนก
        // 60/80/90) — null/ไม่มี key = ใช้ค่า saved/default ตามปกติ
        private async Task<GoldStageSummary> ComputeGoldStageSummaryAsync(
            string metal, DateTime start, DateTime end, List<DateTime> bucketEnds,
            Dictionary<int, decimal>? stageDraftByWorkerType = null)
        {
            var allRows = await GetGoldStageRowsAsync(start, end, metal);
            var savedTargets = await _goldLossTargetService.GetCurrentTargetsAsync();
            var workerNames = await _workerLookupService.GetWorkerNamesAsync();

            var summary = new GoldStageSummary { AllRows = allRows };

            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                var deptRows = allRows.Where(r => r.DeptKey == deptKey).ToList();
                var stats = ProductionGoldStageEvaluator.ComputeDeptStats(deptKey, deptRows, workerNames);
                summary.DeptStatsByDept[deptKey] = stats;
                summary.SeriesByDept[deptKey] = ProductionGoldStageEvaluator.ComputeSeries(deptKey, stats.NotWeighed, deptRows, start, bucketEnds);

                if (!ProductionGoldStageEvaluator.OutlierExcludedDepts.Contains(deptKey))
                {
                    var returned = deptRows.Where(r => r.IsReturned).ToList();
                    var (attributed, unassigned) = ProductionGoldStageEvaluator.ComputeOutliers(
                        returned, workerNames, ProductionInsightThresholds.GoldStageOutlierMedianMultiplier, ProductionInsightThresholds.GoldStageOutlierMinDiffGram);
                    summary.OutliersAttributedByDept[deptKey] = attributed;
                    summary.OutliersUnassignedByDept[deptKey] = unassigned;
                }
                else
                {
                    summary.OutliersAttributedByDept[deptKey] = new List<ProductionGoldStageEvaluator.OutlierRow>();
                    summary.OutliersUnassignedByDept[deptKey] = new List<ProductionGoldStageEvaluator.OutlierRow>();
                }

                decimal? target = null;
                var targetSource = "saved";
                if (StageDeptTargetWorkerTypeMap.TryGetValue(deptKey, out var stageWt))
                {
                    if (stageDraftByWorkerType != null && stageDraftByWorkerType.TryGetValue(stageWt, out var draftVal))
                    {
                        target = draftVal;
                        targetSource = "draft";
                    }
                    else
                    {
                        target = savedTargets.TryGetValue(("STAGE", stageWt, metal), out var row)
                            ? row.TargetPercent
                            : DefaultStageGoldLossTargetPercent(stageWt, metal);
                        targetSource = "saved";
                    }
                }
                summary.TargetPercentByDept[deptKey] = target;
                summary.TargetSourceByDept[deptKey] = targetSource;

                decimal? slipLoss = null;
                if (StageDeptSlipWorkerTypeMap.TryGetValue(deptKey, out var slipWt))
                {
                    slipLoss = await GetSlipLossPercentAsync(slipWt, metal, start, end);
                }
                summary.SlipLossPercentByDept[deptKey] = slipLoss;
            }

            return summary;
        }

        // ACT_RECEIVE_PENDING/ACT_CHECK_STAGE: เติมทับ param จากข้อมูลที่คำนวณไว้แล้วตรงๆ เสมอ (ไม่พึ่ง finding
        // param ที่อาจไม่ fire) — บทเรียนเดียวกับ Gold.ACT_REVIEW_ALLOWANCE/Capacity
        private static void EnrichGoldStageActionParams(
            List<Wip.ActionItem> actions, GoldStageSummary summary, int pendingOverCount, decimal pendingOverGram)
        {
            foreach (var action in actions)
            {
                switch (action.Code)
                {
                    case "ACT_RECEIVE_PENDING":
                        action.Params["count"] = pendingOverCount;
                        action.Params["gram"] = Math.Round(pendingOverGram, 2);
                        break;
                    case "ACT_CHECK_STAGE":
                    {
                        var worst = summary.DeptStatsByDept.Values
                            .Where(d => summary.TargetPercentByDept.TryGetValue(d.DeptKey, out var t) && t.HasValue && d.DiffPercent.HasValue
                                && d.DiffPercent.Value > t!.Value + ProductionInsightThresholds.GoldStageTargetTolerancePercent)
                            .OrderByDescending(d => d.DiffPercent!.Value - summary.TargetPercentByDept[d.DeptKey]!.Value)
                            .FirstOrDefault();
                        if (worst == null) break;

                        action.Params["deptKey"] = worst.DeptKey;
                        action.Params["diffPercent"] = Math.Round(worst.DiffPercent!.Value, 2);
                        action.Params["targetPercent"] = Math.Round(summary.TargetPercentByDept[worst.DeptKey]!.Value, 2);
                        break;
                    }
                }
            }
        }

        public async Task<GoldByStage.Response> GoldByStage(GoldByStage.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;

            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, "month").Select(b => b.End).ToList();
            var stageDraftByWorkerType = request.DraftTargets?
                .Where(d => d.Scope == "STAGE" && d.Metal == metal)
                .GroupBy(d => d.WorkerType)
                .ToDictionary(g => g.Key, g => g.Last().TargetPercent);
            var summary = await ComputeGoldStageSummaryAsync(metal, start, end, bucketEnds, stageDraftByWorkerType);
            var workerNames = await _workerLookupService.GetWorkerNamesAsync();

            var departmentsOut = new List<GoldByStage.DepartmentItem>();
            var seriesOut = new List<GoldByStage.SeriesItem>();

            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                var stats = summary.DeptStatsByDept[deptKey];
                var deptRows = summary.AllRows.Where(r => r.DeptKey == deptKey).ToList();
                var returnedRows = deptRows.Where(r => r.IsReturned).ToList();
                var topWorkers = ProductionGoldStageEvaluator.ComputeTopWorkers(
                    returnedRows, workerNames, ProductionInsightThresholds.GoldStageWorkerMinRows, ProductionInsightThresholds.GoldStageWorkerTopCount);

                departmentsOut.Add(new GoldByStage.DepartmentItem
                {
                    DeptKey = deptKey,
                    NotWeighed = stats.NotWeighed,
                    IncludesScrap = stats.IncludesScrap,
                    SendGram = stats.SendGram,
                    ReceivedGram = stats.ReceivedGram,
                    ReturnedSendGram = stats.ReturnedSendGram,
                    DiffGram = stats.DiffGram,
                    DiffPercent = stats.DiffPercent,
                    TargetPercent = summary.TargetPercentByDept[deptKey],
                    TargetSource = summary.TargetPercentByDept[deptKey].HasValue ? summary.TargetSourceByDept[deptKey] : null,
                    SlipLossPercent = summary.SlipLossPercentByDept[deptKey],
                    PendingCount = stats.PendingCount,
                    PendingGram = stats.PendingGram,
                    PendingWithWorkerCount = stats.PendingWithWorkerCount,
                    PendingWithWorkerGram = stats.PendingWithWorkerGram,
                    PendingQueueCount = stats.PendingQueueCount,
                    PendingQueueGram = stats.PendingQueueGram,
                    OutlierCount = summary.OutliersAttributedByDept[deptKey].Count,
                    OutlierUnassignedCount = summary.OutliersUnassignedByDept[deptKey].Count,
                    Workers = topWorkers.Select(w => new GoldByStage.WorkerItem
                    {
                        WorkerCode = w.WorkerCode,
                        Rows = w.Rows,
                        ReturnedSendGram = w.ReturnedSendGram,
                        DiffGram = w.DiffGram,
                        DiffPercent = w.DiffPercent
                    }).ToList()
                });

                seriesOut.AddRange(summary.SeriesByDept[deptKey].Select(s => new GoldByStage.SeriesItem
                {
                    BucketEnd = s.BucketEnd,
                    DeptKey = s.DeptKey,
                    DiffPercent = s.DiffPercent,
                    ReturnedSendGram = s.ReturnedSendGram
                }));
            }

            return new GoldByStage.Response { Departments = departmentsOut, Series = seriesOut };
        }

        // row diff% > 2×dept median diff% (เฉพาะแถวคืนแล้วในช่วง) AND diffGram >= 0.10g — ไม่รวม trim/gemSort
        public async Task<DataSourceResult> GoldStageOutlierJobs(GoldStageOutlierJobs.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;

            var allRows = await GetGoldStageRowsAsync(start, end, metal);
            var workerNames = await _workerLookupService.GetWorkerNamesAsync();
            var deptFilter = request.DepartmentKeys != null && request.DepartmentKeys.Length > 0
                ? new HashSet<string>(request.DepartmentKeys)
                : null;

            // แสดงทุกแถว outlier ไม่ว่าจะมีช่างจริงถือครองหรือ placeholder/ไม่มีช่าง (เกณฑ์ unassigned ใช้แค่
            // แยกนับใน GoldByStage.OutlierCount/OutlierUnassignedCount ไม่ได้ใช้ซ่อนแถวจากตารางนี้) — ใส่
            // workerName ให้ UI โชว์ชื่อ/ชื่อ placeholder ได้
            var items = new List<GoldStageOutlierJobs.Item>();
            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                if (ProductionGoldStageEvaluator.OutlierExcludedDepts.Contains(deptKey)) continue;
                if (deptFilter != null && !deptFilter.Contains(deptKey)) continue;

                var returnedRows = allRows.Where(r => r.DeptKey == deptKey && r.IsReturned).ToList();
                var (attributed, unassigned) = ProductionGoldStageEvaluator.ComputeOutliers(
                    returnedRows, workerNames, ProductionInsightThresholds.GoldStageOutlierMedianMultiplier, ProductionInsightThresholds.GoldStageOutlierMinDiffGram);

                foreach (var o in attributed.Concat(unassigned))
                {
                    items.Add(new GoldStageOutlierJobs.Item
                    {
                        PlanId = o.Row.PlanId,
                        Wo = o.Row.Wo ?? string.Empty,
                        WoNumber = o.Row.WoNumber,
                        WoText = o.Row.WoText ?? string.Empty,
                        DeptKey = deptKey,
                        WorkerCode = o.Row.WorkerCode,
                        WorkerName = !string.IsNullOrWhiteSpace(o.Row.WorkerCode) && workerNames.TryGetValue(o.Row.WorkerCode.Trim(), out var wn) ? wn : null,
                        Date = o.Row.DetailRequestDate ?? o.Row.HeaderCreateDate,
                        SendGram = o.Row.SendGram,
                        CheckGram = o.Row.CheckGram ?? 0,
                        DiffGram = o.DiffGram,
                        DiffPercent = o.DiffPercent,
                        DeptMedianPercent = o.DeptMedianPercent
                    });
                }
            }

            IEnumerable<GoldStageOutlierJobs.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DiffPercent);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // งานที่ยังไม่คืน (pending) ในช่วง ไม่กรอง stale — exclude แผนทดสอบเหมือน GoldUncoveredJobs — default
        // (includeQueue=false) แสดงเฉพาะของที่มีช่างจริงถือครอง (pendingWithWorker) — includeQueue=true รวมของ
        // ที่ยังค้างคิว (placeholder/ไม่มีช่าง) ด้วย แต่ละแถวมี isQueue บอกชัดเจน
        public async Task<DataSourceResult> GoldStagePendingReturn(GoldStagePendingReturn.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var metal = IsValidMetal(request.Metal) ? request.Metal : ProductionGoldLossEvaluator.MetalGold;
            var olderThanDays = request.OlderThanDays > 0 ? request.OlderThanDays : ProductionInsightThresholds.GoldStagePendingOlderThanDaysDefault;

            var allRows = await GetGoldStageRowsAsync(start, end, metal);
            var workerNames = await _workerLookupService.GetWorkerNamesAsync();
            var deptFilter = request.DepartmentKeys != null && request.DepartmentKeys.Length > 0
                ? new HashSet<string>(request.DepartmentKeys)
                : null;

            var pendingRows = ProductionGoldStageEvaluator.ComputePendingOverDays(allRows, olderThanDays, now);
            if (deptFilter != null)
            {
                pendingRows = pendingRows.Where(r => deptFilter.Contains(r.DeptKey)).ToList();
            }

            pendingRows = pendingRows.Where(r =>
                !(r.Wo != null && r.Wo.ToUpper().Contains("TEST"))
                && !(r.ProductName != null && r.ProductName.ToUpper().Contains("TEST"))
                && !(r.ProductNumber != null && r.ProductNumber.ToUpper().Contains("TEST"))
                && !(r.CustomerNumber != null && r.CustomerNumber.ToUpper().Contains("TEST"))
            ).ToList();

            // ช่าง TEST (code/name_th มีคำว่า TEST ปน) = ข้อมูลทดสอบ ไม่ใช่คิวรองานที่ถูกต้องตามกระบวนการ —
            // ตัดทิ้งเสมอไม่ว่า includeQueue จะเป็น true/false (ต่างจาก placeholder "รอจ่าย..." ที่ includeQueue=true
            // ยังโชว์ได้ปกติ)
            pendingRows = pendingRows.Where(r => !ProductionGoldStageEvaluator.IsTestWorker(r.WorkerCode, workerNames)).ToList();

            if (!request.IncludeQueue)
            {
                pendingRows = pendingRows.Where(r => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(r.WorkerCode, workerNames)).ToList();
            }

            var items = pendingRows.Select(r => new GoldStagePendingReturn.Item
            {
                PlanId = r.PlanId,
                Wo = r.Wo ?? string.Empty,
                WoNumber = r.WoNumber,
                WoText = r.WoText ?? string.Empty,
                DeptKey = r.DeptKey,
                WorkerCode = r.WorkerCode,
                WorkerName = !string.IsNullOrWhiteSpace(r.WorkerCode) && workerNames.TryGetValue(r.WorkerCode.Trim(), out var wn) ? wn : null,
                IsQueue = ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(r.WorkerCode, workerNames),
                SentDate = r.HeaderCreateDate,
                SendGram = r.SendGram,
                DaysSince = Math.Round((now - r.HeaderCreateDate).TotalDays, 1)
            }).ToList();

            IEnumerable<GoldStagePendingReturn.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.DaysSince);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // ---- ช่างและค่าแรง (Workers) ----

        private class WorkerWageRow
        {
            public string DeptKey { get; set; } = null!;
            public int PlanId { get; set; }
            public string? WorkerCode { get; set; }
            public DateTime RequestDate { get; set; }
            public decimal Wages { get; set; }
            public decimal? GoldWeightSend { get; set; }
            public decimal? GoldWeightCheck { get; set; }
            public string? Gold { get; set; }
        }

        // ฐานเดียวกับ GetGoldStageRowsAsync (status_detail/header, 5 แผนกจ่ายค่าแรง: trim/rawPolish/gemSort/
        // setting/plating) แต่ไม่กรอง gold_weight_send>0/gold!=null (ไม่ใช่ทุกงานมีการชั่งทอง) — ขอบเขตวันที่ =
        // detail.request_date (ตามสั่ง ต่างจาก GetGoldStageRowsAsync ที่ใช้ header.create_date)
        private async Task<List<WorkerWageRow>> GetWorkerWageRowsAsync(DateTime start, DateTime end)
        {
            var statuses = new[] { 50, 60, 70, 80, 90 };

            var raw = await (
                from detail in _jewelryContext.TbtProductionPlanStatusDetail
                join header in _jewelryContext.TbtProductionPlanStatusHeader on detail.HeaderId equals header.Id
                where header.IsActive && detail.IsActive
                    && statuses.Contains(header.Status)
                    && detail.RequestDate != null && detail.RequestDate >= start && detail.RequestDate <= end
                select new
                {
                    header.Status,
                    header.WorkerCode,
                    detail.Worker,
                    detail.WorkerSub,
                    detail.RequestDate,
                    detail.TotalWages,
                    detail.GoldWeightSend,
                    detail.GoldWeightCheck,
                    detail.Gold,
                    detail.ProductionPlanId
                })
                .AsNoTracking()
                .ToListAsync();

            return raw
                .Select(r => new WorkerWageRow
                {
                    DeptKey = ProductionPlanDepartments.DepartmentKeyOf(r.Status) ?? string.Empty,
                    PlanId = r.ProductionPlanId,
                    WorkerCode = !string.IsNullOrWhiteSpace(r.Worker) ? r.Worker : (!string.IsNullOrWhiteSpace(r.WorkerSub) ? r.WorkerSub : r.WorkerCode),
                    RequestDate = r.RequestDate!.Value,
                    Wages = r.TotalWages ?? 0,
                    GoldWeightSend = r.GoldWeightSend,
                    GoldWeightCheck = r.GoldWeightCheck,
                    Gold = r.Gold
                })
                .Where(r => !string.IsNullOrEmpty(r.DeptKey))
                .ToList();
        }

        // โหลด+normalize หน่วย tang ทุกโลหะ (ไม่กรอง metal ในนี้ — caller กรองเอง) — logic เดียวกับที่ใช้ใน
        // Gold()/GetSlipLossPercentAsync (ซ้ำโดยตั้งใจ เหมือนรูปแบบเดิมในไฟล์นี้ที่โหลด tang ซ้ำกันหลายจุด)
        private async Task<List<ProductionGoldLossEvaluator.LossUnit>> GetTangUnitsAsync(DateTime start, DateTime end)
        {
            var tangSlipsRaw = await _jewelryContext.TbtGoldLossTangSlip
                .AsNoTracking()
                .Where(s => s.IsActive && s.RequestDateEnd != null && s.RequestDateEnd >= start && s.RequestDateEnd <= end)
                .ToListAsync();

            var tangItemsBySlip = await GetTangItemsBySlipAsync(tangSlipsRaw.Select(s => s.Id).ToList());
            var tangRows = tangSlipsRaw.Select(s => new ProductionGoldLossEvaluator.TangSlipRow
            {
                SlipId = s.Id,
                DocumentNo = s.DocumentNo,
                WorkerCode = s.WorkerCode,
                WorkerName = s.WorkerName,
                RequestDateStart = s.RequestDateStart,
                RequestDateEnd = s.RequestDateEnd,
                ReturnedTotal = s.ReturnedTotal,
                RawLoss = s.RawLoss,
                AllowedLoss = s.AllowedLoss,
                PricePerGram = s.PricePerGram,
                TotalMoneyDiff = s.TotalMoneyDiff,
                Items = tangItemsBySlip.TryGetValue(s.Id, out var its) ? its : new List<ProductionGoldLossEvaluator.TangSlipItemMetalRow>()
            }).ToList();

            return ProductionGoldLossEvaluator.NormalizeTang(tangRows);
        }

        private static Dictionary<string, decimal?> ComputeSlipLossPercentByWorker(IReadOnlyList<ProductionGoldLossEvaluator.LossUnit> units)
        {
            return units.GroupBy(u => u.WorkerCode).ToDictionary(g => g.Key, g =>
            {
                var received = g.Sum(u => u.ReceivedGram);
                var raw = g.Sum(u => u.RawLossGram);
                return received > 0 ? (decimal?)Math.Round(raw / received * 100, 2) : null;
            });
        }

        private class UnpaidJobRow
        {
            public int PlanId { get; set; }
            public string? Wo { get; set; }
            public int WoNumber { get; set; }
            public string? WoText { get; set; }
            public string DeptKey { get; set; } = null!;
            public string? WorkerCode { get; set; }
            public DateTime JobDate { get; set; }
            public decimal CheckGram { get; set; }
        }

        // piece-rate context = trim(50)/setting(80) หรือช่างที่ employmentType เป็น OUTSIDE/SHOP (แผนกไหนก็ได้ —
        // ช่างร้าน/นอกบ้านอาจโผล่แผนกอื่น เช่น plating) — ต้อง checked แล้ว (gold_weight_check ไม่ว่าง) แต่ยังไม่
        // บันทึกค่าแรง (total_wages ว่าง/0) — ไม่จำกัดเฉพาะ 5 แผนกค่าแรงเหมือน GetWorkerWageRowsAsync
        private async Task<List<UnpaidJobRow>> GetUnpaidPieceJobRowsAsync(
            DateTime start, DateTime end, IReadOnlyDictionary<string, string> workerNames, IReadOnlyDictionary<string, string?> employmentTypes)
        {
            var raw = await (
                from detail in _jewelryContext.TbtProductionPlanStatusDetail
                join header in _jewelryContext.TbtProductionPlanStatusHeader on detail.HeaderId equals header.Id
                where header.IsActive && detail.IsActive
                    && detail.RequestDate != null && detail.RequestDate >= start && detail.RequestDate <= end
                    && detail.GoldWeightCheck != null
                    && (detail.TotalWages == null || detail.TotalWages == 0)
                select new
                {
                    header.Status,
                    header.WorkerCode,
                    detail.Worker,
                    detail.WorkerSub,
                    detail.RequestDate,
                    detail.GoldWeightCheck,
                    detail.ProductionPlanId,
                    header.ProductionPlan.Wo,
                    header.ProductionPlan.WoNumber,
                    header.ProductionPlan.WoText
                })
                .AsNoTracking()
                .ToListAsync();

            var result = new List<UnpaidJobRow>();
            foreach (var r in raw)
            {
                var deptKey = ProductionPlanDepartments.DepartmentKeyOf(r.Status);
                if (deptKey == null) continue;

                var workerCode = !string.IsNullOrWhiteSpace(r.Worker) ? r.Worker : (!string.IsNullOrWhiteSpace(r.WorkerSub) ? r.WorkerSub : r.WorkerCode);
                if (ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(workerCode, workerNames)) continue;

                var employmentType = employmentTypes.TryGetValue(workerCode!.Trim(), out var et) ? et : null;
                var isPieceContext = r.Status == 50 || r.Status == 80 || employmentType == "OUTSIDE" || employmentType == "SHOP";
                if (!isPieceContext) continue;

                result.Add(new UnpaidJobRow
                {
                    PlanId = r.ProductionPlanId,
                    Wo = r.Wo,
                    WoNumber = r.WoNumber,
                    WoText = r.WoText,
                    DeptKey = deptKey,
                    WorkerCode = workerCode,
                    JobDate = r.RequestDate!.Value,
                    CheckGram = r.GoldWeightCheck!.Value
                });
            }

            return result;
        }

        public async Task<Workers.Response> Workers(Workers.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;

            var deptFilter = request.DepartmentKeys != null && request.DepartmentKeys.Length > 0
                ? new HashSet<string>(request.DepartmentKeys)
                : null;
            var empTypeFilter = request.EmploymentTypes != null && request.EmploymentTypes.Length > 0
                ? new HashSet<string>(request.EmploymentTypes, StringComparer.OrdinalIgnoreCase)
                : null;

            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, "month").Select(b => b.End).ToList();
            var bucketDurationDays = new List<double>();
            {
                var prevBoundary = start;
                foreach (var be in bucketEnds)
                {
                    bucketDurationDays.Add((be - prevBoundary).TotalDays);
                    prevBoundary = be;
                }
            }
            var rangeDays = Math.Max((end - start).TotalDays, 1);
            var monthsForRates = Math.Max(rangeDays / 30.44, 0.1);

            var workerNames = await _workerLookupService.GetWorkerNamesAsync();
            var employmentTypes = await _workerLookupService.GetWorkerEmploymentTypesAsync();

            var allRows = await GetWorkerWageRowsAsync(start, end);
            var rows = allRows.Where(r => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(r.WorkerCode, workerNames)).ToList();
            if (deptFilter != null) rows = rows.Where(r => deptFilter.Contains(r.DeptKey)).ToList();
            if (empTypeFilter != null)
            {
                rows = rows.Where(r =>
                {
                    var et = employmentTypes.TryGetValue(r.WorkerCode!.Trim(), out var v) ? v : null;
                    return et != null && empTypeFilter.Contains(et);
                }).ToList();
            }

            // ---- slip-side (SLIP scope) % loss ต่อช่าง — trim จากใบตั้งฉาก(tang)/setting จากใบฝัง GOLD เท่านั้น ----
            var tangUnitsGold = (await GetTangUnitsAsync(start, end)).Where(u => u.Metal == ProductionGoldLossEvaluator.MetalGold).ToList();
            var settingUnitsGold = (await GetSettingUnitsAsync(start, end, null)).Where(u => u.Metal == ProductionGoldLossEvaluator.MetalGold).ToList();
            var tangLossPercentByWorker = ComputeSlipLossPercentByWorker(tangUnitsGold);
            var settingLossPercentByWorker = ComputeSlipLossPercentByWorker(settingUnitsGold);

            // ---- worker × dept table rows ----
            var deptTotalJobs = rows.GroupBy(r => r.DeptKey).ToDictionary(g => g.Key, g => g.Count());
            var workerRows = new List<Workers.WorkerItem>();

            foreach (var g in rows.GroupBy(r => (Code: r.WorkerCode!.Trim(), Dept: r.DeptKey)))
            {
                var list = g.ToList();
                var jobs = list.Count;
                var plans = list.Select(r => r.PlanId).Distinct().Count();
                var wages = list.Sum(r => r.Wages);
                decimal? wagePerJob = jobs > 0 ? Math.Round(wages / jobs, 2) : (decimal?)null;
                var deptTotal = deptTotalJobs.TryGetValue(g.Key.Dept, out var dt) ? dt : 0;
                var shareOfDeptJobs = deptTotal > 0 ? Math.Round((decimal)jobs / deptTotal * 100, 2) : 0m;

                var goldReturned = list.Where(r =>
                    ProductionGoldLossEvaluator.ClassifyMetal(r.Gold) == ProductionGoldLossEvaluator.MetalGold
                    && (r.GoldWeightSend ?? 0) > 0 && r.GoldWeightCheck.HasValue).ToList();
                decimal? goldDiffPercent = null;
                if (goldReturned.Count > 0)
                {
                    var sendSum = goldReturned.Sum(r => r.GoldWeightSend ?? 0);
                    var checkSum = goldReturned.Sum(r => r.GoldWeightCheck ?? 0);
                    if (sendSum > 0) goldDiffPercent = Math.Round((sendSum - checkSum) / sendSum * 100, 2);
                }

                decimal? goldLossPercent = null;
                if (g.Key.Dept == ProductionGoldStageEvaluator.DeptTrim && tangLossPercentByWorker.TryGetValue(g.Key.Code, out var tl)) goldLossPercent = tl;
                else if (g.Key.Dept == ProductionGoldStageEvaluator.DeptSetting && settingLossPercentByWorker.TryGetValue(g.Key.Code, out var sl)) goldLossPercent = sl;

                var employmentType = employmentTypes.TryGetValue(g.Key.Code, out var et2) ? et2 : null;
                var name = workerNames.TryGetValue(g.Key.Code, out var nm) ? nm : g.Key.Code;

                workerRows.Add(new Workers.WorkerItem
                {
                    Code = g.Key.Code,
                    Name = name,
                    DeptKey = g.Key.Dept,
                    EmploymentType = employmentType,
                    Jobs = jobs,
                    Plans = plans,
                    Wages = Math.Round(wages, 2),
                    WagePerJob = wagePerJob,
                    ShareOfDeptJobs = shareOfDeptJobs,
                    GoldLossPercent = goldLossPercent,
                    GoldDiffPercent = goldDiffPercent,
                    IsRateOutlier = false,
                    IsConcentrated = false
                });
            }

            // ---- WRK_RATE_OUTLIER: wagePerJob > 3×median ของกลุ่มแผนก+ประเภทการจ้างเดียวกัน (jobs >= 5) ----
            var medianByGroup = workerRows
                .Where(w => w.WagePerJob.HasValue)
                .GroupBy(w => (w.DeptKey, Emp: w.EmploymentType ?? "UNKNOWN"))
                .ToDictionary(g => g.Key, g => (decimal)ProductionStageLeadTimeEvaluator.Median(g.Select(x => (double)x.WagePerJob!.Value).ToList()));

            var rateOutliers = new List<(Workers.WorkerItem Item, decimal MedianPerJob)>();
            foreach (var w in workerRows)
            {
                if (w.Jobs < ProductionInsightThresholds.WrkRateOutlierMinJobs || !w.WagePerJob.HasValue) continue;
                if (!medianByGroup.TryGetValue((w.DeptKey, w.EmploymentType ?? "UNKNOWN"), out var median) || median <= 0) continue;
                if (w.WagePerJob.Value <= median * ProductionInsightThresholds.WrkRateOutlierMultiplier) continue;

                w.IsRateOutlier = true;
                rateOutliers.Add((w, median));
            }

            // ---- concentration รายแผนก (top2Share ตามจำนวนงาน) ----
            var concentration = new List<Workers.ConcentrationItem>();
            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                var deptWorkers = workerRows.Where(w => w.DeptKey == deptKey).OrderByDescending(w => w.Jobs).ToList();
                var deptTotal = deptWorkers.Sum(w => w.Jobs);
                var top2 = deptWorkers.Take(2).ToList();
                var top2Share = deptTotal > 0 ? Math.Round((decimal)top2.Sum(w => w.Jobs) / deptTotal * 100, 2) : 0m;

                concentration.Add(new Workers.ConcentrationItem
                {
                    DeptKey = deptKey,
                    Top2Share = top2Share,
                    TopWorkers = top2.Select(w => new Workers.ConcentrationWorkerItem
                    {
                        Code = w.Code,
                        Name = w.Name,
                        Share = deptTotal > 0 ? Math.Round((decimal)w.Jobs / deptTotal * 100, 2) : 0m
                    }).ToList()
                });

                if (deptWorkers.Count >= ProductionInsightThresholds.WrkConcentrationMinWorkers && top2Share >= ProductionInsightThresholds.WrkConcentrationTop2SharePercent)
                {
                    foreach (var w in top2) w.IsConcentrated = true;
                }
            }

            // ---- series รายแผนก + รวม (wagePerPlan ใช้ first95Dates — "ผลิตเสร็จ" เดียวกับ Capacity.kpi.outputPerMonth) ----
            var trendData = await _trendDataProvider.GetTrendDataAsync();
            var first95Dates = ProductionCapacityEvaluator.ComputeFirst95Dates(trendData.Headers);

            var seriesOut = new List<Workers.SeriesItem>();
            foreach (var deptKey in ProductionGoldStageEvaluator.AllDepartments)
            {
                var deptRows = rows.Where(r => r.DeptKey == deptKey).ToList();
                var prevD = start;
                foreach (var bucketEnd in bucketEnds)
                {
                    var w = deptRows.Where(r => r.RequestDate > prevD && r.RequestDate <= bucketEnd).Sum(r => r.Wages);
                    seriesOut.Add(new Workers.SeriesItem { BucketEnd = bucketEnd, DeptKey = deptKey, Wages = Math.Round(w, 2) });
                    prevD = bucketEnd;
                }
            }

            var seriesTotalOut = new List<Workers.SeriesTotalItem>();
            var fullMonthWagePerPlan = new List<(bool IsFullMonth, decimal? WagePerPlan)>();
            var prevT = start;
            for (var i = 0; i < bucketEnds.Count; i++)
            {
                var bucketEnd = bucketEnds[i];
                var bucketWages = rows.Where(r => r.RequestDate > prevT && r.RequestDate <= bucketEnd).Sum(r => r.Wages);
                var bucketOutput = first95Dates.Values.Count(d => d > prevT && d <= bucketEnd);
                decimal? wagePerPlan = bucketOutput > 0 ? Math.Round(bucketWages / bucketOutput, 2) : (decimal?)null;
                var isFullMonth = bucketDurationDays[i] >= ProductionInsightThresholds.WrkFullMonthMinDays;

                seriesTotalOut.Add(new Workers.SeriesTotalItem { BucketEnd = bucketEnd, Wages = Math.Round(bucketWages, 2), Output = bucketOutput, WagePerPlan = wagePerPlan });
                fullMonthWagePerPlan.Add((isFullMonth, wagePerPlan));
                prevT = bucketEnd;
            }

            // ---- unpaid piece jobs (ไม่กรองตาม request.DepartmentKeys/EmploymentTypes — ขอบเขตกว้างกว่าตาราง workers) ----
            var unpaidRows = await GetUnpaidPieceJobRowsAsync(start, end, workerNames, employmentTypes);
            var unpaidCount = unpaidRows.Count;

            // ---- WRK_GOLD_REPEAT: reuse เกณฑ์ GOLD_REPEAT_OFFENDER (tang 50 + setting 80, GOLD) — targetPercent
            // ไม่มีผลต่อ OverBuckets/QualifyingBuckets (เทียบ allowance ของตัวเอง A ไม่ใช่เป้าหมายบริษัท) ใส่ 0 ได้ ----
            var tangWorkerResults = ProductionGoldLossEvaluator.ComputeWorkers(ProductionGoldLossEvaluator.WorkerTypeTang, tangUnitsGold, 0m, start, bucketEnds);
            var settingWorkerResults = ProductionGoldLossEvaluator.ComputeWorkers(ProductionGoldLossEvaluator.WorkerTypeSetting, settingUnitsGold, 0m, start, bucketEnds);
            var goldRepeatOffenders = tangWorkerResults.Concat(settingWorkerResults)
                .Where(w => w.QualifyingBuckets >= ProductionInsightThresholds.GoldRepeatOffenderMinBuckets && w.OverBuckets == w.QualifyingBuckets)
                .Where(w => !ProductionGoldStageEvaluator.IsQueueOrEmptyWorker(w.WorkerCode, workerNames))
                .Select(w => (Code: w.WorkerCode, Name: w.WorkerName ?? w.WorkerCode))
                .ToList();

            // ---- kpi ----
            var totalWages = rows.Sum(r => r.Wages);
            var wagesPerMonth = Math.Round(totalWages / (decimal)monthsForRates, 2);
            var wagesByDeptOut = ProductionGoldStageEvaluator.AllDepartments.Select(d =>
            {
                var deptWages = rows.Where(r => r.DeptKey == d).Sum(r => r.Wages);
                return new Workers.DeptWageItem
                {
                    DeptKey = d,
                    WagesPerMonth = Math.Round(deptWages / (decimal)monthsForRates, 2),
                    Share = totalWages > 0 ? Math.Round(deptWages / totalWages * 100, 2) : 0m
                };
            }).ToList();

            var outputInRange = first95Dates.Values.Count(d => d > start && d <= end);
            decimal? wagePerOutputPlan = outputInRange > 0 ? Math.Round(totalWages / outputInRange, 2) : (decimal?)null;

            var activeWorkers = rows.Select(r => r.WorkerCode!.Trim()).Distinct().Count();
            var activeWorkersByDeptOut = ProductionGoldStageEvaluator.AllDepartments.Select(d => new Workers.DeptCountItem
            {
                DeptKey = d,
                Count = rows.Where(r => r.DeptKey == d).Select(r => r.WorkerCode!.Trim()).Distinct().Count()
            }).ToList();

            var outsideShopWages = rows.Where(r =>
            {
                var et = employmentTypes.TryGetValue(r.WorkerCode!.Trim(), out var v) ? v : null;
                return et == "OUTSIDE" || et == "SHOP";
            }).Sum(r => r.Wages);
            var outsideWageShare = totalWages > 0 ? Math.Round(outsideShopWages / totalWages * 100, 2) : 0m;

            // ---- FC_KEY_PERSON_RISK: แผนกกระจุกตัวที่สุด (top2Share สูงสุด, มีช่างจริงอย่างน้อย 1 คน) — reuse
            // pipeline เดียวกับ Capacity ทุกจุด (visits, standardByDept, activeWipByDept, BuildCapacity) เพื่อให้
            // queueDaysNow ตรงกับที่ผู้ใช้เห็นในแท็บ Capacity เป๊ะ (เทียบกันได้โดยตรง) ไม่ใช้ตัวประมาณจาก jobs/rangeDays
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIdsForWip = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));
            var activeWipByDept = ComputeActiveWipByDept(openPlans, stalePlanIdsForWip);

            var visits = ProductionStageLeadTimeEvaluator.ComputeVisits(trendData.Plans, trendData.Headers, ProductionPlanDepartments.Departments, now);
            var savedStandards = await _stageStandardService.GetCurrentStandardsAsync();
            var standardByDept = ProductionPlanDepartments.Departments.ToDictionary(
                d => d.Key,
                d => savedStandards.TryGetValue(d.Key, out var s) ? s.StandardDays : ProductionInsightThresholds.DefaultStageStandardDays);
            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(visits, ProductionPlanDepartments.Departments, standardByDept, activeWipByDept, start, end);

            var problems = new List<Wip.Finding>();
            var forecasts = new List<Wip.Finding>();

            foreach (var c in concentration)
            {
                var deptWorkerCount = workerRows.Count(w => w.DeptKey == c.DeptKey);
                AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWrkConcentration(
                    c.DeptKey, deptWorkerCount, c.Top2Share, c.TopWorkers.Select(w => (w.Code, w.Name)).ToList()));
            }

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWrkRateOutlier(
                rateOutliers.Select(o => (o.Item.Code, o.Item.Name, o.Item.WagePerJob!.Value, o.MedianPerJob)).ToList()));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateWrkWagePerPlanRising(fullMonthWagePerPlan));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWrkUnpaidJobs(unpaidCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateWrkGoldRepeat(goldRepeatOffenders));

            var fullMonthWages = fullMonthWagePerPlan
                .Zip(seriesTotalOut, (f, s) => (f.IsFullMonth, s.Wages))
                .Where(x => x.IsFullMonth)
                .Select(x => x.Wages)
                .ToList();
            var lastThreeFullMonthWages = fullMonthWages.Count >= 3 ? fullMonthWages.Skip(fullMonthWages.Count - 3).ToList() : fullMonthWages;
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcWagesNextMonth(lastThreeFullMonthWages));

            var mostConcentrated = concentration.Where(c => c.TopWorkers.Count > 0).OrderByDescending(c => c.Top2Share).FirstOrDefault();
            if (mostConcentrated != null)
            {
                var topWorker = mostConcentrated.TopWorkers[0];
                var deptKeyRisk = mostConcentrated.DeptKey;
                var capRow = capacity.Departments.FirstOrDefault(d => d.Key == deptKeyRisk);
                var topShareRatio = (double)(topWorker.Share / 100m);

                double? queueDaysNow = capRow?.QueueDaysCurrent;
                double? queueDaysWithout = null;
                if (capRow != null && capRow.ExitedPerDay > 0 && topShareRatio < 1.0)
                {
                    var exitsWithout = capRow.ExitedPerDay * (1 - topShareRatio);
                    queueDaysWithout = exitsWithout > 0 ? capRow.ActiveWip / exitsWithout : (double?)null;
                }

                AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcKeyPersonRisk(deptKeyRisk, topWorker.Name, queueDaysNow, queueDaysWithout));
            }

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            var concentrationQualifying = concentration
                .Where(c => workerRows.Count(w => w.DeptKey == c.DeptKey) >= ProductionInsightThresholds.WrkConcentrationMinWorkers
                    && c.Top2Share >= ProductionInsightThresholds.WrkConcentrationTop2SharePercent)
                .ToList();
            EnrichWorkerActionParams(actions, concentrationQualifying, rateOutliers.Select(o => o.Item).ToList(), unpaidCount, goldRepeatOffenders);

            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Workers.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                Kpi = new Workers.KpiData
                {
                    WagesPerMonth = wagesPerMonth,
                    WagesByDept = wagesByDeptOut,
                    WagePerOutputPlan = wagePerOutputPlan,
                    ActiveWorkers = activeWorkers,
                    ActiveWorkersByDept = activeWorkersByDeptOut,
                    OutsideWageShare = outsideWageShare,
                    UnpaidPieceJobs = unpaidCount,
                    ExcludesSalaried = true
                },
                Series = seriesOut,
                SeriesTotal = seriesTotalOut,
                Workers = workerRows,
                Concentration = concentration
            };
        }

        private static void EnrichWorkerActionParams(
            List<Wip.ActionItem> actions,
            List<Workers.ConcentrationItem> concentrationQualifying,
            List<Workers.WorkerItem> rateOutliers,
            int unpaidCount,
            List<(string Code, string Name)> goldRepeatWorkers)
        {
            foreach (var action in actions)
            {
                switch (action.Code)
                {
                    case "ACT_CROSS_TRAIN":
                    {
                        var worst = concentrationQualifying.OrderByDescending(c => c.Top2Share).FirstOrDefault();
                        action.Params["deptKey"] = worst?.DeptKey ?? string.Empty;
                        action.Params["workerNames"] = worst?.TopWorkers.Select(w => w.Name).ToList() ?? new List<string>();
                        break;
                    }
                    case "ACT_REVIEW_RATE":
                        action.Params["workers"] = rateOutliers.Take(3).Select(w => new Dictionary<string, object>
                        {
                            ["code"] = w.Code,
                            ["name"] = w.Name,
                            ["wagePerJob"] = w.WagePerJob ?? 0m
                        }).ToList();
                        break;
                    case "ACT_RECORD_WAGES":
                        action.Params["count"] = unpaidCount;
                        break;
                    case "ACT_TALK_WORKER_GOLD":
                        action.Params["workers"] = goldRepeatWorkers.Select(w => new Dictionary<string, object>
                        {
                            ["code"] = w.Code,
                            ["name"] = w.Name
                        }).ToList();
                        break;
                }
            }
        }

        public async Task<WorkerMonthly.Response> WorkerMonthly(WorkerMonthly.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, "month").Select(b => b.End).ToList();

            var allRows = await GetWorkerWageRowsAsync(start, end);
            var code = (request.Code ?? string.Empty).Trim();
            var rows = allRows
                .Where(r => string.Equals(r.DeptKey, request.DeptKey, StringComparison.Ordinal))
                .Where(r => !string.IsNullOrWhiteSpace(r.WorkerCode) && string.Equals(r.WorkerCode.Trim(), code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var series = new List<WorkerMonthly.SeriesItem>();
            var prev = start;
            foreach (var bucketEnd in bucketEnds)
            {
                var bucketRows = rows.Where(r => r.RequestDate > prev && r.RequestDate <= bucketEnd).ToList();
                var jobs = bucketRows.Count;
                var plans = bucketRows.Select(r => r.PlanId).Distinct().Count();
                var wages = Math.Round(bucketRows.Sum(r => r.Wages), 2);

                var goldReturned = bucketRows.Where(r =>
                    ProductionGoldLossEvaluator.ClassifyMetal(r.Gold) == ProductionGoldLossEvaluator.MetalGold
                    && (r.GoldWeightSend ?? 0) > 0 && r.GoldWeightCheck.HasValue).ToList();
                decimal? goldDiffPercent = null;
                if (goldReturned.Count > 0)
                {
                    var sendSum = goldReturned.Sum(r => r.GoldWeightSend ?? 0);
                    var checkSum = goldReturned.Sum(r => r.GoldWeightCheck ?? 0);
                    if (sendSum > 0) goldDiffPercent = Math.Round((sendSum - checkSum) / sendSum * 100, 2);
                }

                series.Add(new WorkerMonthly.SeriesItem { BucketEnd = bucketEnd, Jobs = jobs, Plans = plans, Wages = wages, GoldDiffPercent = goldDiffPercent });
                prev = bucketEnd;
            }

            return new WorkerMonthly.Response { Series = series };
        }

        public async Task<DataSourceResult> UnpaidPieceJobs(UnpaidPieceJobs.Request request)
        {
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;

            var workerNames = await _workerLookupService.GetWorkerNamesAsync();
            var employmentTypes = await _workerLookupService.GetWorkerEmploymentTypesAsync();
            var rows = await GetUnpaidPieceJobRowsAsync(start, end, workerNames, employmentTypes);

            if (request.DepartmentKeys != null && request.DepartmentKeys.Length > 0)
            {
                var deptFilter = new HashSet<string>(request.DepartmentKeys);
                rows = rows.Where(r => deptFilter.Contains(r.DeptKey)).ToList();
            }

            var items = rows.Select(r => new UnpaidPieceJobs.Item
            {
                PlanId = r.PlanId,
                Wo = r.Wo ?? string.Empty,
                WoNumber = r.WoNumber,
                WoText = r.WoText ?? string.Empty,
                DeptKey = r.DeptKey,
                WorkerCode = r.WorkerCode,
                WorkerName = !string.IsNullOrWhiteSpace(r.WorkerCode) && workerNames.TryGetValue(r.WorkerCode.Trim(), out var wn) ? wn : r.WorkerCode,
                JobDate = r.JobDate,
                CheckGram = r.CheckGram
            }).ToList();

            IEnumerable<UnpaidPieceJobs.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.JobDate);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        // ---- วัตถุดิบที่กระทบการผลิต (Materials — gems only) ----
        // tbt_stock_gold ใช้ไม่ได้ (ติดลบเพราะไม่มีการบันทึกรับเข้า) — หน้านี้ทำเฉพาะพลอย (tbt_stock_gem) —
        // การจับคู่เป็นค่าประมาณ (approximate): ไม่รู้เกรด/คุณภาพละเอียด ไม่หัก reservation ของแผนอื่นออกจาก
        // quantity ที่เห็น (ดู ProductionMaterialGemEvaluator)

        private class MaterialLineResult
        {
            public int PlanId { get; set; }
            public string? Gem { get; set; }
            public string? Shape { get; set; }
            public string? Size { get; set; }
            public string Metal { get; set; } = null!;
            public decimal RequiredQty { get; set; }
            public ProductionMaterialGemEvaluator.MatchResult Match { get; set; } = null!;
        }

        private class MaterialContext
        {
            // สถานะ 69/70 (แผนกตัดพลอย), ไม่ stale, ยังไม่มี type-7 issue เลย
            public List<OpenPlanRow> WaitingPlans { get; set; } = new List<OpenPlanRow>();

            // สถานะ 10,49,50,59,60,69,70 (ยังไม่ผ่านแผนกตัดพลอย), ไม่ stale — ฐานของ Lines ด้านล่าง
            public List<OpenPlanRow> RelevantPlans { get; set; } = new List<OpenPlanRow>();

            // แผน -> วันที่เข้าแผนกตัดพลอยครั้งแรก (header แรกที่ status 69 หรือ 70) — ทุกแผนใน WaitingPlans ต้องมี key นี้เสมอ
            public Dictionary<int, DateTime> GemSortEntryDateByPlan { get; set; } = new Dictionary<int, DateTime>();

            // แผน -> วันที่เข้าสถานะ 70 (ทำงานจริง) ครั้งแรก — ALL ประวัติ (ไม่กรองสถานะปัจจุบัน) ใช้กับ KPI entered→issue
            public Dictionary<int, DateTime> First70DateByPlan { get; set; } = new Dictionary<int, DateTime>();

            // แผน -> วันที่เบิกพลอยจริง (type 7) ครั้งแรก — ALL ประวัติ, join ด้วย wo+wo_number
            public Dictionary<int, DateTime> FirstIssueDateByPlan { get; set; } = new Dictionary<int, DateTime>();

            // 1 แถวต่อบรรทัดวัตถุดิบพลอย (เฉพาะของ RelevantPlans) พร้อมผลจับคู่ stock แล้ว
            public List<MaterialLineResult> Lines { get; set; } = new List<MaterialLineResult>();
        }

        private async Task<MaterialContext> BuildMaterialContextAsync(DateTime now)
        {
            var openPlans = await _wipHelper.GetOpenPlansAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var staleCutoff = now.AddDays(-ProductionInsightThresholds.DefaultStaleDays);
            var stalePlanIds = new HashSet<int>(
                openPlans.Where(p => ProductionPlanDepartments.LastMoveOf(p, lastMoveDates) < staleCutoff).Select(p => p.Id));

            var notPastGemSortStatuses = new HashSet<int> { 10, 49, 50, 59, 60, 69, 70 };
            var gemSortStatuses = new HashSet<int> { 69, 70 };

            var relevantPlans = openPlans.Where(p => notPastGemSortStatuses.Contains(p.Status) && !stalePlanIds.Contains(p.Id)).ToList();
            var relevantPlanIds = relevantPlans.Select(p => p.Id).ToList();

            // MIN(create_date) ต่อแผน ที่ header.status IN (69,70) — "เข้าแผนกตัดพลอย" ระดับแผนก (รอ หรือ ทำงาน)
            var gemSortEntryRows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.IsActive == true && (h.Status == 69 || h.Status == 70))
                .GroupBy(h => h.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, EnteredDate = g.Min(h => h.CreateDate) })
                .ToListAsync();
            var gemSortEntryDateByPlan = gemSortEntryRows.ToDictionary(x => x.PlanId, x => x.EnteredDate);

            // MIN(create_date) ต่อแผน ที่ header.status = 70 เท่านั้น (ทำงานจริง) — ALL ประวัติ ใช้กับ KPI entered→issue
            var first70Rows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => h.IsActive == true && h.Status == 70)
                .GroupBy(h => h.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, EnteredDate = g.Min(h => h.CreateDate) })
                .ToListAsync();
            var first70DateByPlan = first70Rows.ToDictionary(x => x.PlanId, x => x.EnteredDate);

            // type 7 = เบิกพลอยจริงให้แผน (มี wo/wo_number) — join plan ด้วย (wo, wo_number) ตามรูปแบบเดียวกับ
            // StockGemService — ALL ประวัติ (ไม่ bound ด้วย start/end เพราะต้องรู้ "ไม่เคยมี issue เลย" จริงๆ)
            var type7Raw = await (
                from trans in _jewelryContext.TbtStockGemTransection
                where trans.Type == 7 && trans.ProductionPlanWo != null
                join plan in _jewelryContext.TbtProductionPlan
                    on new { Wo = trans.ProductionPlanWo, WoNumber = trans.ProductionPlanWoNumber ?? 0 }
                    equals new { Wo = plan.Wo, WoNumber = plan.WoNumber }
                select new { PlanId = plan.Id, trans.CreateDate })
                .AsNoTracking()
                .ToListAsync();
            var firstIssueDateByPlan = type7Raw.GroupBy(t => t.PlanId).ToDictionary(g => g.Key, g => g.Min(t => t.CreateDate));

            var waitingPlans = relevantPlans.Where(p => gemSortStatuses.Contains(p.Status) && !firstIssueDateByPlan.ContainsKey(p.Id)).ToList();

            var stockList = await _materialGemDataProvider.GetStockGemsAsync();
            var gemNames = await _materialGemDataProvider.GetGemNamesAsync();

            var materialRows = await _jewelryContext.TbtProductionPlanMaterial
                .AsNoTracking()
                .Where(m => m.IsActive == true && m.Gem != null && relevantPlanIds.Contains(m.ProductionPlanId))
                .Select(m => new { m.ProductionPlanId, m.Gold, m.Gem, m.GemShape, m.GemSize, m.GemQty })
                .ToListAsync();

            var lines = materialRows.Select(m =>
            {
                var metal = ProductionGoldLossEvaluator.ClassifyMetal(m.Gold);
                var requiredQty = (decimal)(m.GemQty ?? 0);
                var match = ProductionMaterialGemEvaluator.Match(m.Gem, m.GemShape, m.GemSize, metal, requiredQty, stockList, gemNames);
                return new MaterialLineResult
                {
                    PlanId = m.ProductionPlanId,
                    Gem = m.Gem,
                    Shape = m.GemShape,
                    Size = m.GemSize,
                    Metal = metal,
                    RequiredQty = requiredQty,
                    Match = match
                };
            }).ToList();

            return new MaterialContext
            {
                WaitingPlans = waitingPlans,
                RelevantPlans = relevantPlans,
                GemSortEntryDateByPlan = gemSortEntryDateByPlan,
                First70DateByPlan = first70DateByPlan,
                FirstIssueDateByPlan = firstIssueDateByPlan,
                Lines = lines
            };
        }

        // รวมจำนวนเบิก (type 7) ต่อรหัส stock ใน 90 วันล่าสุดจาก now — ใช้กับ MaterialGemDemand.usedPerMonth และ
        // MaterialGemLowCover ทั้งคู่ (คนละมุมมองของข้อมูลเดียวกัน)
        private async Task<Dictionary<string, decimal>> GetGemUsage90dByCodeAsync(DateTime now)
        {
            var windowStart = now.AddDays(-ProductionInsightThresholds.MatGemUsageWindowDays);
            var rows = await _jewelryContext.TbtStockGemTransection
                .AsNoTracking()
                .Where(t => t.Type == 7 && t.CreateDate >= windowStart && t.CreateDate <= now)
                .GroupBy(t => t.Code)
                .Select(g => new { Code = g.Key, Used = g.Sum(x => x.Qty) })
                .ToListAsync();

            return rows.ToDictionary(x => x.Code, x => x.Used);
        }

        private async Task<List<(ProductionMaterialGemEvaluator.StockGemRow Stock, decimal Used90d, double CoverDays)>> ComputeGemCoverRowsAsync(DateTime now)
        {
            var stockList = await _materialGemDataProvider.GetStockGemsAsync();
            var usageByCode = await GetGemUsage90dByCodeAsync(now);

            var result = new List<(ProductionMaterialGemEvaluator.StockGemRow, decimal, double)>();
            foreach (var s in stockList)
            {
                if (s.Quantity <= 0) continue;
                if (!usageByCode.TryGetValue(s.Code, out var used90d) || used90d <= 0) continue;

                var dailyUsage = used90d / ProductionInsightThresholds.MatGemUsageWindowDays;
                var coverDays = (double)(s.Quantity / dailyUsage);
                result.Add((s, used90d, coverDays));
            }

            return result;
        }

        public async Task<Materials.Response> Materials(Materials.Request request)
        {
            var now = DateTime.UtcNow;
            var start = request.Start.UtcDateTime;
            var end = request.End.UtcDateTime;
            var bucket = string.IsNullOrWhiteSpace(request.Bucket) ? "month" : request.Bucket;
            var bucketEnds = ProductionPlanTrendEvaluator.GenerateBuckets(start, end, bucket).Select(b => b.End).ToList();

            var ctx = await BuildMaterialContextAsync(now);

            // entered(70) ในช่วง [start,end] -> delay ถึง issue จริงครั้งแรก (เฉพาะคู่ที่มี issue แล้ว, issue >= entered)
            var entries = ctx.First70DateByPlan
                .Where(kv => kv.Value > start && kv.Value <= end)
                .Select(kv => new
                {
                    PlanId = kv.Key,
                    EnteredDate = kv.Value,
                    Issue = ctx.FirstIssueDateByPlan.TryGetValue(kv.Key, out var id) && id >= kv.Value ? (DateTime?)id : null
                })
                .ToList();

            var allDelayDays = entries.Where(e => e.Issue.HasValue).Select(e => (e.Issue!.Value - e.EnteredDate).TotalDays).ToList();
            double? issueMedianDays = allDelayDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Median(allDelayDays) : (double?)null;
            double? issueP90Days = allDelayDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Percentile(allDelayDays, 90) : (double?)null;

            var seriesOut = new List<Materials.SeriesItem>();
            var prev = start;
            foreach (var bucketEnd in bucketEnds)
            {
                var bucketEntries = entries.Where(e => e.EnteredDate > prev && e.EnteredDate <= bucketEnd).ToList();
                var bucketDelays = bucketEntries.Where(e => e.Issue.HasValue).Select(e => (e.Issue!.Value - e.EnteredDate).TotalDays).ToList();

                seriesOut.Add(new Materials.SeriesItem
                {
                    BucketEnd = bucketEnd,
                    Entered = bucketEntries.Count,
                    IssueMedianDays = bucketDelays.Count > 0 ? Math.Round(ProductionStageLeadTimeEvaluator.Median(bucketDelays), 1) : (double?)null,
                    IssueP90Days = bucketDelays.Count > 0 ? Math.Round(ProductionStageLeadTimeEvaluator.Percentile(bucketDelays, 90), 1) : (double?)null
                });
                prev = bucketEnd;
            }

            // waiting — snapshot ตอนนี้ (ไม่ขึ้นกับ start/end)
            var waitingDaysList = ctx.WaitingPlans
                .Select(p => (now - (ctx.GemSortEntryDateByPlan.TryGetValue(p.Id, out var ed) ? ed : p.CreateDate)).TotalDays)
                .ToList();
            double? waitingMedianDays = waitingDaysList.Count > 0 ? ProductionStageLeadTimeEvaluator.Median(waitingDaysList) : (double?)null;

            // สถานะบรรทัด/แผน
            var totalLines = ctx.Lines.Count;
            var unmatchedLines = ctx.Lines.Count(l => l.Match.Status == ProductionMaterialGemEvaluator.StatusUnmatched);
            var unmatchedByGem = ctx.Lines.Count(l => l.Match.UnmatchedReason == "gem");
            var unmatchedBySpec = ctx.Lines.Count(l => l.Match.UnmatchedReason == "spec");
            var shortLinesCount = ctx.Lines.Count(l => l.Match.Status == ProductionMaterialGemEvaluator.StatusShort);
            var matchedLines = totalLines - unmatchedLines;

            var statusByPlan = ctx.Lines
                .GroupBy(l => l.PlanId)
                .ToDictionary(g => g.Key, g => ProductionMaterialGemEvaluator.WorstStatus(g.Select(x => x.Match.Status)));
            var readyPlansCount = statusByPlan.Values.Count(s => s == ProductionMaterialGemEvaluator.StatusReady);
            var shortPlansCount = statusByPlan.Values.Count(s => s == ProductionMaterialGemEvaluator.StatusShort);

            // "ready แต่ยังไม่เบิก" — เฉพาะแผนที่กำลังรออยู่แผนกตัดพลอยจริง (waitingPlans) และทุกบรรทัดพร้อม —
            // แคบกว่า kpi.readyPlans ตรงๆ (readyPlans รวมแผนที่ยังไม่ถึงแผนกตัดพลอยด้วย ซึ่งยังเบิกไม่ได้อยู่แล้วตามขั้นตอน)
            var waitingPlanIdSet = ctx.WaitingPlans.Select(p => p.Id).ToHashSet();
            var readyWaitingCount = statusByPlan.Count(kv => waitingPlanIdSet.Contains(kv.Key) && kv.Value == ProductionMaterialGemEvaluator.StatusReady);

            // แผนที่ยังไม่ถึงแผนกตัดพลอย (10,49,50,59,60) ที่ต้องการ spec ที่ "short" อยู่แล้วตอนนี้ — เตือนล่วงหน้า
            var upcomingStatuses = new HashSet<int> { 10, 49, 50, 59, 60 };
            var upcomingPlanIds = new HashSet<int>(ctx.RelevantPlans.Where(p => upcomingStatuses.Contains(p.Status)).Select(p => p.Id));
            var upcomingShortLines = ctx.Lines.Where(l => upcomingPlanIds.Contains(l.PlanId) && l.Match.Status == ProductionMaterialGemEvaluator.StatusShort).ToList();
            var upcomingShortPlansCount = upcomingShortLines.Select(l => l.PlanId).Distinct().Count();

            var coverRows = await ComputeGemCoverRowsAsync(now);
            var lowCoverCount = coverRows.Count(r => r.CoverDays < ProductionInsightThresholds.MatGemLowCoverDefaultDays);

            var problems = new List<Wip.Finding>();
            var forecasts = new List<Wip.Finding>();

            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateMatGemWaiting(ctx.WaitingPlans.Count, waitingMedianDays));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateMatReadyNotIssued(readyWaitingCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateMatGemShort(shortPlansCount, shortLinesCount));
            AddIfNotNull(problems, ProductionInsightRuleEngine.EvaluateMatSpecUnmatched(unmatchedLines, totalLines));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcGemShortUpcoming(upcomingShortPlansCount, upcomingShortLines.Count));
            AddIfNotNull(forecasts, ProductionInsightRuleEngine.EvaluateFcGemStockout(lowCoverCount, ProductionInsightThresholds.MatGemLowCoverDefaultDays));

            var actions = ProductionInsightRuleEngine.BuildActions(problems, forecasts);
            EnrichMaterialActionParams(actions, readyWaitingCount, shortLinesCount, unmatchedLines);

            var worstSeverity = ProductionInsightRuleEngine.WorstSeverity(problems.Concat(forecasts));
            var status = worstSeverity == "critical" || worstSeverity == "warning" ? worstSeverity : "ok";

            return new Materials.Response
            {
                AsOf = now,
                Status = status,
                Problems = problems,
                Forecasts = forecasts,
                Actions = actions,
                Kpi = new Materials.KpiData
                {
                    WaitingPlans = ctx.WaitingPlans.Count,
                    WaitingMedianDays = waitingMedianDays.HasValue ? Math.Round(waitingMedianDays.Value, 1) : (double?)null,
                    IssueMedianDays = issueMedianDays.HasValue ? Math.Round(issueMedianDays.Value, 1) : (double?)null,
                    IssueP90Days = issueP90Days.HasValue ? Math.Round(issueP90Days.Value, 1) : (double?)null,
                    ReadyPlans = readyPlansCount,
                    ReadyWaitingPlans = readyWaitingCount,
                    ShortPlans = shortPlansCount,
                    ShortLines = shortLinesCount,
                    UnmatchedLines = unmatchedLines,
                    UnmatchedByGem = unmatchedByGem,
                    UnmatchedBySpec = unmatchedBySpec,
                    MatchedLines = matchedLines,
                    TotalLines = totalLines,
                    LowCoverCount = lowCoverCount
                },
                Series = seriesOut
            };
        }

        private static void EnrichMaterialActionParams(List<Wip.ActionItem> actions, int readyWaitingCount, int shortLines, int unmatchedLines)
        {
            foreach (var action in actions)
            {
                switch (action.Code)
                {
                    case "ACT_ISSUE_READY":
                        action.Params["count"] = readyWaitingCount;
                        break;
                    case "ACT_BUY_GEMS":
                        action.Params["lines"] = shortLines;
                        break;
                    case "ACT_FIX_GEM_SPEC":
                        action.Params["lines"] = unmatchedLines;
                        break;
                    case "ACT_ADD_GEM_SORTER":
                        action.Params["deptKey"] = "gemSort";
                        break;
                }
            }
        }

        public async Task<DataSourceResult> MaterialWaitingPlans(MaterialWaitingPlans.Request request)
        {
            var now = DateTime.UtcNow;
            var ctx = await BuildMaterialContextAsync(now);
            var statusNames = await _wipHelper.GetStatusNamesAsync();
            var lastMoveDates = await _wipHelper.GetLastMoveDatesAsync();
            var linesByPlan = ctx.Lines.GroupBy(l => l.PlanId).ToDictionary(g => g.Key, g => g.ToList());

            var items = ctx.WaitingPlans.Select(p =>
            {
                var enteredDate = ctx.GemSortEntryDateByPlan.TryGetValue(p.Id, out var ed) ? ed : p.CreateDate;
                var lines = linesByPlan.TryGetValue(p.Id, out var ls) ? ls : new List<MaterialLineResult>();
                var gems = lines.Select(l => new MaterialWaitingPlans.GemLineItem
                {
                    Gem = l.Gem,
                    Shape = l.Shape,
                    Size = l.Size,
                    Metal = l.Metal,
                    Qty = (int)l.RequiredQty,
                    Available = l.Match.AvailableQty,
                    Status = l.Match.Status,
                    UnmatchedReason = l.Match.UnmatchedReason
                }).ToList();
                var gemStatus = gems.Count > 0 ? ProductionMaterialGemEvaluator.WorstStatus(gems.Select(g => g.Status)) : ProductionMaterialGemEvaluator.StatusUnmatched;
                var lastMoveDate = ProductionPlanDepartments.LastMoveOf(p, lastMoveDates);

                return new MaterialWaitingPlans.Item
                {
                    PlanId = p.Id,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    StatusId = p.Status,
                    StatusName = statusNames.TryGetValue(p.Status, out var sn) ? sn : null,
                    DepartmentKey = ProductionPlanDepartments.DepartmentKeyOf(p.Status),
                    CreateDate = p.CreateDate,
                    LastMoveDate = lastMoveDate,
                    DaysSinceMove = (int)Math.Round((now - lastMoveDate).TotalDays),
                    RequestDate = p.RequestDate,
                    EnteredGemSortDate = enteredDate,
                    WaitingDays = Math.Round((now - enteredDate).TotalDays, 1),
                    Gems = gems,
                    GemStatus = gemStatus
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(request.GemStatus))
            {
                var filterStatus = request.GemStatus.Trim().ToLowerInvariant();
                items = items.Where(i => i.GemStatus == filterStatus).ToList();
            }

            IEnumerable<MaterialWaitingPlans.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderByDescending(x => x.WaitingDays);
            }

            var dataSource = ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);

            var pageItems = dataSource.Data?.Cast<MaterialWaitingPlans.Item>().ToList() ?? new List<MaterialWaitingPlans.Item>();
            if (pageItems.Count > 0)
            {
                var plansById = ctx.WaitingPlans.ToDictionary(p => p.Id);
                var pagePlans = pageItems.Select(i => plansById[i.PlanId]).ToList();
                var lastActionInfo = await _wipHelper.GetLastActionInfoAsync(pagePlans);

                foreach (var item in pageItems)
                {
                    if (!lastActionInfo.TryGetValue(item.PlanId, out var info)) continue;
                    item.LastUpdateBy = info.LastUpdateBy;
                    item.LastAction = info.LastAction;
                    item.LastActionRemark = info.LastActionRemark;
                    item.LastActionDate = info.LastActionDate;
                    item.Workers = info.Workers;
                    item.WorkerItems = info.WorkerItems.Select(w => new StalePlans.WorkerItem { Code = w.Code, Name = w.Name, IsQueue = w.IsQueue }).ToList();
                }

                dataSource.Data = pageItems;
            }

            return dataSource;
        }

        public async Task<DataSourceResult> MaterialGemDemand(MaterialGemDemand.Request request)
        {
            var now = DateTime.UtcNow;
            var ctx = await BuildMaterialContextAsync(now);
            var usageByCode = await GetGemUsage90dByCodeAsync(now);
            var stockList = await _materialGemDataProvider.GetStockGemsAsync();
            var gemNames = await _materialGemDataProvider.GetGemNamesAsync();

            var gemSortStatuses = new HashSet<int> { 69, 70 };
            var upcomingStatuses = new HashSet<int> { 10, 49, 50, 59, 60 };
            var planStatusById = ctx.RelevantPlans.ToDictionary(p => p.Id, p => p.Status);

            var specGroups = ctx.Lines.GroupBy(l => (
                Gem: (l.Gem ?? string.Empty).Trim().ToUpperInvariant(),
                Shape: ProductionMaterialGemEvaluator.Normalize(l.Shape),
                Size: ProductionMaterialGemEvaluator.Normalize(l.Size),
                l.Metal));

            var items = new List<MaterialGemDemand.Item>();
            foreach (var g in specGroups)
            {
                var requiredQty = g.Sum(l => l.RequiredQty);
                var planIds = g.Select(l => l.PlanId).Distinct().ToList();
                var waitingPlansCount = planIds.Count(id => planStatusById.TryGetValue(id, out var st) && gemSortStatuses.Contains(st));
                var upcomingPlansCount = planIds.Count(id => planStatusById.TryGetValue(id, out var st) && upcomingStatuses.Contains(st));

                var rep = g.First();
                var match = ProductionMaterialGemEvaluator.Match(rep.Gem, rep.Shape, rep.Size, rep.Metal, requiredQty, stockList, gemNames);

                decimal? usedPerMonth = null;
                if (match.MatchedStockCodes.Count > 0)
                {
                    var used90d = match.MatchedStockCodes.Sum(c => usageByCode.TryGetValue(c, out var u) ? u : 0m);
                    if (used90d > 0) usedPerMonth = Math.Round(used90d / ProductionInsightThresholds.MatGemUsageWindowMonths, 2);
                }

                double? coverDays = null;
                if (usedPerMonth.HasValue && usedPerMonth.Value > 0 && match.AvailableQty.HasValue)
                {
                    coverDays = (double)(match.AvailableQty.Value / (usedPerMonth.Value / 30m));
                }

                items.Add(new MaterialGemDemand.Item
                {
                    Gem = rep.Gem,
                    Shape = rep.Shape,
                    Size = rep.Size,
                    Metal = rep.Metal,
                    WaitingPlans = waitingPlansCount,
                    UpcomingPlans = upcomingPlansCount,
                    RequiredQty = requiredQty,
                    Available = match.AvailableQty,
                    UsedPerMonth = usedPerMonth,
                    CoverDays = coverDays.HasValue ? Math.Round(coverDays.Value, 1) : (double?)null,
                    Status = match.Status,
                    UnmatchedReason = match.UnmatchedReason
                });
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var filterStatus = request.Status.Trim().ToLowerInvariant();
                items = items.Where(i => i.Status == filterStatus).ToList();
            }

            IEnumerable<MaterialGemDemand.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items
                    .OrderByDescending(x => x.Status == ProductionMaterialGemEvaluator.StatusShort ? 2 : (x.Status == ProductionMaterialGemEvaluator.StatusUnmatched ? 1 : 0))
                    .ThenByDescending(x => x.RequiredQty);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        public async Task<DataSourceResult> MaterialGemLowCover(MaterialGemLowCover.Request request)
        {
            var now = DateTime.UtcNow;
            var coverRows = await ComputeGemCoverRowsAsync(now);
            var thresholdDays = request.CoverDays > 0 ? request.CoverDays : ProductionInsightThresholds.MatGemLowCoverDefaultDays;

            var items = coverRows
                .Where(r => r.CoverDays < thresholdDays)
                .Select(r => new MaterialGemLowCover.Item
                {
                    Code = r.Stock.Code,
                    GroupName = r.Stock.GroupName,
                    Shape = r.Stock.Shape,
                    Size = r.Stock.Size,
                    Grade = r.Stock.Grade,
                    Quantity = r.Stock.Quantity,
                    Used90d = r.Used90d,
                    CoverDays = Math.Round(r.CoverDays, 1)
                }).ToList();

            IEnumerable<MaterialGemLowCover.Item> ordered = items;
            if (request.Sort == null || !request.Sort.Any())
            {
                ordered = items.OrderBy(x => x.CoverDays);
            }

            return ordered.ToDataSourceResult(request.Take, request.Skip, request.Sort, request.Group);
        }

        private static void AddIfNotNull(List<Wip.Finding> list, Wip.Finding? finding)
        {
            if (finding != null) list.Add(finding);
        }
    }
}
