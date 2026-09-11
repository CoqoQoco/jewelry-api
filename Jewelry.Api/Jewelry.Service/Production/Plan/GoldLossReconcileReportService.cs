using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Plan
{
    public class GoldLossReconcileReportService : BaseService, IGoldLossReconcileReportService
    {
        private readonly JewelryContext _jewelryContext;

        public GoldLossReconcileReportService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<jewelry.Model.Production.Plan.GoldLossReconcileReport.SearchResponse> GetGoldLossReconcileReport(
            jewelry.Model.Production.Plan.GoldLossReconcileReport.SearchRequest request)
        {
            var castingStatus = jewelry.Model.Constant.ProductionPlanStatus.Casting;
            var embeddStatus = jewelry.Model.Constant.ProductionPlanStatus.Embedd;

            // 1. ช่วงวันที่ (default 6 เดือนล่าสุด รวมเดือนปัจจุบัน, เวลาไทย ICT UTC+7)
            var nowIct = DateTime.UtcNow.AddHours(7);
            var currentMonthStartIct = new DateTimeOffset(new DateTime(nowIct.Year, nowIct.Month, 1, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.FromHours(7));
            var defaultStart = currentMonthStartIct.AddMonths(-5);
            var defaultEnd = currentMonthStartIct.AddMonths(1);

            var start = request.Start ?? defaultStart;
            var end = request.End ?? defaultEnd;

            // ใช้ plain UTC DateTime สำหรับเทียบกับคอลัมน์ DateTime/DateTime? ของ EF model ตรงๆ
            // (เลี่ยงปัญหา nullable DateTime vs DateTimeOffset translate ไม่ได้)
            var startUtc = start.UtcDateTime;
            var endUtc = end.UtcDateTime;

            var statuses = (request.Status != null && request.Status.Length > 0)
                ? request.Status
                : new[] { castingStatus, embeddStatus };

            var workerCode = string.IsNullOrWhiteSpace(request.WorkerCode) ? null : request.WorkerCode.Trim();

            // 2. plan-side: ดึงแถวดิบ (ที่จ่ายทองจริง) ในช่วง+สถานะที่ขอ แล้วแยก returned/pending + linkage ใน memory
            var planFullRows = await (from detail in _jewelryContext.TbtProductionPlanStatusDetail
                                      join header in _jewelryContext.TbtProductionPlanStatusHeader
                                          on detail.HeaderId equals header.Id
                                      where header.IsActive == true
                                          && detail.IsActive == true
                                          && detail.GoldWeightSend > 0
                                          && statuses.Contains(header.Status)
                                          && header.CreateDate >= startUtc
                                          && header.CreateDate < endUtc
                                      select new
                                      {
                                          header.Status,
                                          header.CreateDate,
                                          detail.Worker,
                                          GoldWeightSend = detail.GoldWeightSend ?? 0,
                                          GoldWeightCheck = detail.GoldWeightCheck ?? 0,
                                          IsReturned = detail.GoldWeightCheck.HasValue,
                                          detail.GoldLossTangSlipId,
                                          detail.WorkerGoldLossSlipId
                                      }).ToListAsync();

            var planRowsFiltered = workerCode == null
                ? planFullRows
                : planFullRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.Worker) && string.Equals(x.Worker.Trim(), workerCode, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var planBuckets = planRowsFiltered
                .Select(x => new
                {
                    Year = x.CreateDate.AddHours(7).Year,
                    Month = x.CreateDate.AddHours(7).Month,
                    x.Status,
                    x.GoldWeightSend,
                    x.GoldWeightCheck,
                    x.IsReturned,
                    IsLinked = x.Status == castingStatus ? x.GoldLossTangSlipId != null : x.WorkerGoldLossSlipId != null
                })
                .GroupBy(x => new { x.Year, x.Month, x.Status })
                .ToDictionary(g => (g.Key.Year, g.Key.Month, g.Key.Status), g => new
                {
                    PlanSumSend = g.Where(x => x.IsReturned).Sum(x => x.GoldWeightSend),
                    PlanSumCheck = g.Where(x => x.IsReturned).Sum(x => x.GoldWeightCheck),
                    PlanRowsReturned = g.Count(x => x.IsReturned),
                    PlanRowsPending = g.Count(x => !x.IsReturned),
                    LinkedDetailRows = g.Count(x => x.IsLinked),
                    UnlinkedDetailRows = g.Count(x => !x.IsLinked)
                });

            // 3. slip-side ฝั่งช่างแต่ง (status 50) — tbt_gold_loss_tang_slip (+ extras)
            var tangBuckets = new Dictionary<(int Year, int Month), (int SlipCount, decimal SlipIssued, decimal SlipReturned, decimal SlipRawLoss, decimal SlipAllowedLoss, decimal SlipDiffLoss, decimal SlipMoneyDiff)>();
            var tangExtraBuckets = new Dictionary<(int Year, int Month), (decimal ExtraIssuedWeight, decimal ExtraReturnedWeight, decimal ExtraReturnedNotCounted)>();

            if (statuses.Contains(castingStatus))
            {
                var tangHeaderRows = await _jewelryContext.TbtGoldLossTangSlip
                    .Where(x => x.IsActive == true
                        && x.RequestDateEnd != null
                        && x.RequestDateEnd >= startUtc
                        && x.RequestDateEnd < endUtc)
                    .Select(x => new
                    {
                        x.Id,
                        x.RequestDateEnd,
                        x.WorkerCode,
                        x.IssuedTotal,
                        x.ReturnedTotal,
                        x.RawLoss,
                        x.AllowedLoss,
                        x.DiffLoss,
                        x.TotalMoneyDiff
                    })
                    .ToListAsync();

                var tangHeaderFiltered = workerCode == null
                    ? tangHeaderRows
                    : tangHeaderRows
                        .Where(x => !string.IsNullOrWhiteSpace(x.WorkerCode) && string.Equals(x.WorkerCode.Trim(), workerCode, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                tangBuckets = tangHeaderFiltered
                    .GroupBy(x => (Year: x.RequestDateEnd!.Value.AddHours(7).Year, Month: x.RequestDateEnd!.Value.AddHours(7).Month))
                    .ToDictionary(g => g.Key, g => (
                        SlipCount: g.Count(),
                        SlipIssued: g.Sum(x => x.IssuedTotal ?? 0),
                        SlipReturned: g.Sum(x => x.ReturnedTotal ?? 0),
                        SlipRawLoss: g.Sum(x => x.RawLoss ?? 0),
                        SlipAllowedLoss: g.Sum(x => x.AllowedLoss ?? 0),
                        SlipDiffLoss: g.Sum(x => x.DiffLoss ?? 0),
                        SlipMoneyDiff: g.Sum(x => x.TotalMoneyDiff ?? 0)
                    ));

                var tangHeaderIds = tangHeaderFiltered.Select(x => x.Id).ToList();

                var tangExtraRows = await _jewelryContext.TbtGoldLossTangSlipExtra
                    .Where(x => x.IsActive == true && tangHeaderIds.Contains(x.SlipId))
                    .Select(x => new { x.SlipId, x.Kind, x.Weight, x.CountInCalc })
                    .ToListAsync();

                var tangHeaderLookup = tangHeaderFiltered.ToDictionary(x => x.Id, x => x.RequestDateEnd!.Value);

                tangExtraBuckets = tangExtraRows
                    .Where(x => tangHeaderLookup.ContainsKey(x.SlipId))
                    .Select(x => new
                    {
                        RequestDateEnd = tangHeaderLookup[x.SlipId],
                        x.Kind,
                        x.Weight,
                        x.CountInCalc
                    })
                    .GroupBy(x => (Year: x.RequestDateEnd.AddHours(7).Year, Month: x.RequestDateEnd.AddHours(7).Month))
                    .ToDictionary(g => g.Key, g => (
                        ExtraIssuedWeight: g.Where(x => x.Kind == 1 && x.CountInCalc).Sum(x => x.Weight ?? 0),
                        ExtraReturnedWeight: g.Where(x => x.Kind == 2 && x.CountInCalc).Sum(x => x.Weight ?? 0),
                        ExtraReturnedNotCounted: g.Where(x => x.Kind == 2 && !x.CountInCalc).Sum(x => x.Weight ?? 0)
                    ));
            }

            // 4. slip-side ฝั่งช่างฝัง (status 80) — tbt_worker_gold_loss_slip (+ items + returns)
            var workerBuckets = new Dictionary<(int Year, int Month), (int SlipCount, decimal SlipIssued, decimal SlipReturned, decimal SlipRawLoss, decimal SlipAllowedLoss, decimal SlipDiffLoss, decimal SlipMoneyDiff, decimal ExtraReturnedWeight, decimal ExtraReturnedNotCounted)>();

            if (statuses.Contains(embeddStatus))
            {
                var workerHeaderRows = await _jewelryContext.TbtWorkerGoldLossSlip
                    .Where(x => x.IsActive == true
                        && x.RequestDateEnd >= startUtc
                        && x.RequestDateEnd < endUtc)
                    .Select(x => new { x.Id, x.RequestDateEnd, x.WorkerCode, x.TotalMoneyDiff })
                    .ToListAsync();

                var workerHeaderFiltered = workerCode == null
                    ? workerHeaderRows
                    : workerHeaderRows
                        .Where(x => !string.IsNullOrWhiteSpace(x.WorkerCode) && string.Equals(x.WorkerCode.Trim(), workerCode, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                var workerHeaderIds = workerHeaderFiltered.Select(x => x.Id).ToList();

                var workerItemRows = await _jewelryContext.TbtWorkerGoldLossSlipItem
                    .Where(x => x.IsActive == true && workerHeaderIds.Contains(x.SlipId))
                    .Select(x => new { x.SlipId, x.GoldWeightSend, x.GoldWeightCheck, x.WeightLossAllowed })
                    .ToListAsync();

                var workerReturnRows = await _jewelryContext.TbtWorkerGoldLossSlipReturn
                    .Where(x => workerHeaderIds.Contains(x.SlipId))
                    .Select(x => new { x.SlipId, x.Weight, x.CountInCalc })
                    .ToListAsync();

                workerBuckets = workerHeaderFiltered
                    .GroupBy(x => (Year: x.RequestDateEnd.AddHours(7).Year, Month: x.RequestDateEnd.AddHours(7).Month))
                    .ToDictionary(g => g.Key, g =>
                    {
                        var ids = g.Select(x => x.Id).ToHashSet();
                        var items = workerItemRows.Where(i => ids.Contains(i.SlipId)).ToList();
                        var returns = workerReturnRows.Where(r => ids.Contains(r.SlipId)).ToList();

                        var issued = items.Sum(i => i.GoldWeightSend ?? 0);
                        var itemReturned = items.Sum(i => i.GoldWeightCheck ?? 0);
                        var extraReturnedCounted = returns.Where(r => r.CountInCalc).Sum(r => r.Weight);
                        var extraReturnedNotCounted = returns.Where(r => !r.CountInCalc).Sum(r => r.Weight);
                        var returned = itemReturned + extraReturnedCounted;
                        var rawLoss = issued - returned;
                        var allowedLoss = items.Sum(i => i.WeightLossAllowed ?? 0);
                        var diffLoss = allowedLoss - rawLoss;
                        var moneyDiff = g.Sum(x => x.TotalMoneyDiff ?? 0);

                        return (
                            SlipCount: g.Count(),
                            SlipIssued: issued,
                            SlipReturned: returned,
                            SlipRawLoss: rawLoss,
                            SlipAllowedLoss: allowedLoss,
                            SlipDiffLoss: diffLoss,
                            SlipMoneyDiff: moneyDiff,
                            ExtraReturnedWeight: extraReturnedCounted,
                            ExtraReturnedNotCounted: extraReturnedNotCounted
                        );
                    });
            }

            // 5. enumerate ทุกเดือนในช่วง x ทุก status ที่ขอ (โชว์ครบแม้ไม่มีข้อมูล จะได้เห็นแนวโน้มต่อเนื่อง)
            var startIct = startUtc.AddHours(7);
            var lastInclusiveIct = endUtc.AddHours(7).AddTicks(-1);
            var startIndex = startIct.Year * 12 + (startIct.Month - 1);
            var endIndex = lastInclusiveIct.Year * 12 + (lastInclusiveIct.Month - 1);
            if (endIndex < startIndex) endIndex = startIndex;

            var statusMaster = await _jewelryContext.TbmProductionPlanStatus.ToListAsync();

            var rows = new List<jewelry.Model.Production.Plan.GoldLossReconcileReport.GoldLossReconcileRow>();

            for (var idx = startIndex; idx <= endIndex; idx++)
            {
                var year = idx / 12;
                var month = idx % 12 + 1;

                foreach (var statusCode in statuses)
                {
                    var master = statusMaster.FirstOrDefault(m => m.Id == statusCode);
                    planBuckets.TryGetValue((year, month, statusCode), out var plan);

                    var slipCount = 0;
                    decimal slipIssued = 0, slipReturned = 0, slipRawLoss = 0, slipAllowedLoss = 0, slipDiffLoss = 0, slipMoneyDiff = 0;
                    decimal extraIssued = 0, extraReturned = 0, extraReturnedNotCounted = 0;

                    if (statusCode == castingStatus && tangBuckets.TryGetValue((year, month), out var tang))
                    {
                        slipCount = tang.SlipCount;
                        slipIssued = tang.SlipIssued;
                        slipReturned = tang.SlipReturned;
                        slipRawLoss = tang.SlipRawLoss;
                        slipAllowedLoss = tang.SlipAllowedLoss;
                        slipDiffLoss = tang.SlipDiffLoss;
                        slipMoneyDiff = tang.SlipMoneyDiff;
                    }

                    if (statusCode == castingStatus && tangExtraBuckets.TryGetValue((year, month), out var extra))
                    {
                        extraIssued = extra.ExtraIssuedWeight;
                        extraReturned = extra.ExtraReturnedWeight;
                        extraReturnedNotCounted = extra.ExtraReturnedNotCounted;
                    }

                    if (statusCode == embeddStatus && workerBuckets.TryGetValue((year, month), out var worker))
                    {
                        slipCount = worker.SlipCount;
                        slipIssued = worker.SlipIssued;
                        slipReturned = worker.SlipReturned;
                        slipRawLoss = worker.SlipRawLoss;
                        slipAllowedLoss = worker.SlipAllowedLoss;
                        slipDiffLoss = worker.SlipDiffLoss;
                        slipMoneyDiff = worker.SlipMoneyDiff;
                        extraReturned = worker.ExtraReturnedWeight;
                        extraReturnedNotCounted = worker.ExtraReturnedNotCounted;
                    }

                    var planSumSend = plan?.PlanSumSend ?? 0m;
                    var planSumCheck = plan?.PlanSumCheck ?? 0m;
                    var planRawLoss = planSumSend - planSumCheck;
                    var planLossPercent = planSumSend > 0 ? Math.Round(planRawLoss / planSumSend * 100, 2) : 0;
                    var planRowsReturned = plan?.PlanRowsReturned ?? 0;
                    var planRowsPending = plan?.PlanRowsPending ?? 0;
                    var linked = plan?.LinkedDetailRows ?? 0;
                    var unlinked = plan?.UnlinkedDetailRows ?? 0;
                    var totalLinkable = linked + unlinked;

                    var slipLossPercent = slipIssued > 0 ? Math.Round(slipRawLoss / slipIssued * 100, 2) : 0;

                    var gapWeight = planRawLoss - slipRawLoss;
                    var gapExplainedByExtras = extraReturned - extraIssued;
                    var gapUnexplained = gapWeight - gapExplainedByExtras;

                    rows.Add(new jewelry.Model.Production.Plan.GoldLossReconcileReport.GoldLossReconcileRow
                    {
                        Year = year,
                        Month = month,
                        StatusCode = statusCode,
                        StatusName = master?.NameTh ?? statusCode.ToString(),

                        PlanSumSend = planSumSend,
                        PlanSumCheck = planSumCheck,
                        PlanRawLoss = planRawLoss,
                        PlanLossPercent = planLossPercent,
                        PlanRowsReturned = planRowsReturned,
                        PlanRowsPending = planRowsPending,

                        SlipCount = slipCount,
                        SlipIssued = slipIssued,
                        SlipReturned = slipReturned,
                        SlipRawLoss = slipRawLoss,
                        SlipAllowedLoss = slipAllowedLoss,
                        SlipDiffLoss = slipDiffLoss,
                        SlipMoneyDiff = slipMoneyDiff,
                        SlipLossPercent = slipLossPercent,

                        ExtraIssuedWeight = extraIssued,
                        ExtraReturnedWeight = extraReturned,
                        ExtraReturnedNotCounted = extraReturnedNotCounted,
                        GapWeight = gapWeight,
                        GapExplainedByExtras = gapExplainedByExtras,
                        GapUnexplained = gapUnexplained,

                        LinkedDetailRows = linked,
                        UnlinkedDetailRows = unlinked,
                        LinkCoveragePercent = totalLinkable > 0 ? Math.Round((decimal)linked / totalLinkable * 100, 2) : 0
                    });
                }
            }

            rows = rows.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.StatusCode).ToList();

            var sumPlanSend = rows.Sum(x => x.PlanSumSend);
            var sumPlanRawLoss = rows.Sum(x => x.PlanRawLoss);
            var sumSlipIssued = rows.Sum(x => x.SlipIssued);
            var sumSlipRawLoss = rows.Sum(x => x.SlipRawLoss);
            var sumLinked = rows.Sum(x => x.LinkedDetailRows);
            var sumUnlinked = rows.Sum(x => x.UnlinkedDetailRows);
            var sumLinkable = sumLinked + sumUnlinked;

            var summary = new jewelry.Model.Production.Plan.GoldLossReconcileReport.GoldLossReconcileSummary
            {
                PlanSumSend = sumPlanSend,
                PlanSumCheck = rows.Sum(x => x.PlanSumCheck),
                PlanRawLoss = sumPlanRawLoss,
                PlanLossPercent = sumPlanSend > 0 ? Math.Round(sumPlanRawLoss / sumPlanSend * 100, 2) : 0,
                PlanRowsReturned = rows.Sum(x => x.PlanRowsReturned),
                PlanRowsPending = rows.Sum(x => x.PlanRowsPending),

                SlipCount = rows.Sum(x => x.SlipCount),
                SlipIssued = sumSlipIssued,
                SlipReturned = rows.Sum(x => x.SlipReturned),
                SlipRawLoss = sumSlipRawLoss,
                SlipAllowedLoss = rows.Sum(x => x.SlipAllowedLoss),
                SlipDiffLoss = rows.Sum(x => x.SlipDiffLoss),
                SlipMoneyDiff = rows.Sum(x => x.SlipMoneyDiff),
                SlipLossPercent = sumSlipIssued > 0 ? Math.Round(sumSlipRawLoss / sumSlipIssued * 100, 2) : 0,

                ExtraIssuedWeight = rows.Sum(x => x.ExtraIssuedWeight),
                ExtraReturnedWeight = rows.Sum(x => x.ExtraReturnedWeight),
                ExtraReturnedNotCounted = rows.Sum(x => x.ExtraReturnedNotCounted),
                GapWeight = rows.Sum(x => x.GapWeight),
                GapExplainedByExtras = rows.Sum(x => x.GapExplainedByExtras),
                GapUnexplained = rows.Sum(x => x.GapUnexplained),

                LinkedDetailRows = sumLinked,
                UnlinkedDetailRows = sumUnlinked,
                LinkCoveragePercent = sumLinkable > 0 ? Math.Round((decimal)sumLinked / sumLinkable * 100, 2) : 0
            };

            return new jewelry.Model.Production.Plan.GoldLossReconcileReport.SearchResponse
            {
                Start = start,
                End = end,
                Rows = rows,
                Summary = summary
            };
        }
    }
}
