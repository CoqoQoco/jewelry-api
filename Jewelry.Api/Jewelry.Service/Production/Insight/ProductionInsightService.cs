using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Jewelry.Service.Production.Shared;
using Jewelry.Service.Report.Executive;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public ProductionInsightService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IProductionPlanWipHelper wipHelper,
            IExecutiveReportService executiveReportService,
            IProductionPlanTrendDataProvider trendDataProvider,
            IProductionStageStandardService stageStandardService,
            IProductionDeliveryDataProvider deliveryDataProvider,
            IProductionDeliveryTargetService deliveryTargetService)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _wipHelper = wipHelper;
            _executiveReportService = executiveReportService;
            _trendDataProvider = trendDataProvider;
            _stageStandardService = stageStandardService;
            _deliveryDataProvider = deliveryDataProvider;
            _deliveryTargetService = deliveryTargetService;
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

            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(
                visits, ProductionPlanDepartments.Departments, standardByDeptForCapacity, start, end);

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
            var capacity = ProductionStageLeadTimeEvaluator.BuildCapacity(ctx.Visits, ProductionPlanDepartments.Departments, standardByDept, medianWindowStart, now);
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

        private static void AddIfNotNull(List<Wip.Finding> list, Wip.Finding? finding)
        {
            if (finding != null) list.Add(finding);
        }
    }
}
