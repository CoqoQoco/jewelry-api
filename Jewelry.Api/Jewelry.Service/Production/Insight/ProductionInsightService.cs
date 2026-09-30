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

namespace Jewelry.Service.Production.Insight
{
    public class ProductionInsightService : BaseService, IProductionInsightService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IProductionPlanWipHelper _wipHelper;
        private readonly IExecutiveReportService _executiveReportService;
        private readonly IProductionPlanTrendDataProvider _trendDataProvider;
        private readonly IProductionStageStandardService _stageStandardService;

        public ProductionInsightService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IProductionPlanWipHelper wipHelper,
            IExecutiveReportService executiveReportService,
            IProductionPlanTrendDataProvider trendDataProvider,
            IProductionStageStandardService stageStandardService)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _wipHelper = wipHelper;
            _executiveReportService = executiveReportService;
            _trendDataProvider = trendDataProvider;
            _stageStandardService = stageStandardService;
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

        private static void AddIfNotNull(List<Wip.Finding> list, Wip.Finding? finding)
        {
            if (finding != null) list.Add(finding);
        }
    }
}
