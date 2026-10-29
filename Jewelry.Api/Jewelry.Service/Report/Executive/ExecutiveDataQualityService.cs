using Jewelry.Service.Common;
using Jewelry.Service.Helper;
using Jewelry.Service.Material.Insight;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataQuality = jewelry.Model.Report.Executive.DataQuality;

namespace Jewelry.Service.Report.Executive
{
    public class ExecutiveDataQualityService : IExecutiveDataQualityService
    {
        public const string CacheEndpoint = "DataQuality:Overview";

        public static string CacheKey(string metal) => InsightCache.Key(CacheEndpoint, metal);

        private const int MaterialMonths = 12;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InsightCache _insightCache;
        private readonly ILogger<ExecutiveDataQualityService> _logger;

        public ExecutiveDataQualityService(IServiceScopeFactory scopeFactory, InsightCache insightCache, ILogger<ExecutiveDataQualityService> logger)
        {
            _scopeFactory = scopeFactory;
            _insightCache = insightCache;
            _logger = logger;
        }

        // ผลที่มี check โหลดล้มเหลว ห้ามเข้า cache — ส่งผลออกทาง exception (InsightCache ไม่ cache factory ที่ throw)
        private class IncompleteException : Exception
        {
            public IncompleteException(DataQuality.Response response) { Response = response; }
            public DataQuality.Response Response { get; }
        }

        public async Task<DataQuality.Response> DataQuality(DataQuality.Request request)
        {
            var metal = MetalClassifier.Normalize(request?.Metal);
            var thresholds = ExecutiveDataQualityThresholds.Resolve(request?.Thresholds);
            try
            {
                return await _insightCache.GetOrCreateAsync(CacheKey(metal), InsightCache.OverviewTtl, request?.Thresholds, async () =>
                {
                    var (response, failed) = await BuildAsync(metal, thresholds);
                    if (failed) throw new IncompleteException(response);
                    return response;
                });
            }
            catch (IncompleteException ex)
            {
                return ex.Response;
            }
        }

        // ทำงานต่อเนื่องทีละกลุ่ม แต่ละกลุ่มมี DI scope ของตัวเอง — JewelryContext ไม่ thread-safe
        private async Task<(DataQuality.Response Response, bool Failed)> BuildAsync(string metal, DataQuality.ThresholdValues thresholds)
        {
            var inputs = new List<DataQualityInput>();
            var failed = false;

            try
            {
                inputs.AddRange(await LoadSummaryChecks(metal));
            }
            catch (Exception ex)
            {
                failed = true;
                _logger.LogError(ex, "ExecutiveDataQuality: summary-based checks failed");
            }

            try
            {
                inputs.Add(await LoadNegativeGold());
            }
            catch (Exception ex)
            {
                failed = true;
                _logger.LogError(ex, "ExecutiveDataQuality: negative gold check failed");
            }

            // PLACEHOLDER_WORKER_WIP: ยังไม่มีนิยามที่ยืนยันกับข้อมูล prod ได้ -> ไม่ใส่ input = available=false (ไม่นับเป็น failure)
            var checks = ExecutiveDataQualityCalculator.BuildChecks(inputs, thresholds);
            var problems = ExecutiveDataQualityRuleEngine.BuildProblems(checks);

            var response = new DataQuality.Response
            {
                AsOf = DateTime.UtcNow,
                Metal = metal,
                Status = ExecutiveDataQualityCalculator.WorstSeverity(checks),
                Thresholds = thresholds,
                Kpi = ExecutiveDataQualityCalculator.BuildKpi(checks),
                Checks = checks,
                Problems = problems,
                Forecasts = new List<jewelry.Model.Production.Insight.Wip.Finding>(),
                Actions = ExecutiveDataQualityRuleEngine.BuildActions(problems)
            };
            return (response, failed);
        }

        // รายการต้นทุนขาดนับเป็นล็อต (แถวที่ต้องเข้าไปแก้) ไม่ใช่ชิ้น
        // ใช้ Summary (cached 60s) ที่คำนวณเลขเหล่านี้อยู่แล้ว — scope metal เดียวกันกับหน้าอื่น
        private async Task<List<DataQualityInput>> LoadSummaryChecks(string metal)
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IExecutiveReportService>();
            var s = await svc.Summary(new jewelry.Model.Report.Executive.Summary.Request { Metal = metal });

            return new List<DataQualityInput>
            {
                new() { Code = ExecutiveDataQualityCalculator.NoCost, Count = s.Stock.NoCostLotCount, Total = s.Stock.InStockLotCount },
                new() { Code = ExecutiveDataQualityCalculator.InvoiceNoDue, Count = s.Receivables.NoDueDateCount, Total = s.Receivables.InvoiceCount },
                // นับเฉพาะ SO เปิดที่ยังไม่มีใบแจ้งหนี้ (ตามนิยามของ Summary) -> ฐานคือ SO ที่ยังไม่มีบิล
                new() { Code = ExecutiveDataQualityCalculator.SoNoDeliveryDate, Count = s.SalesOrders.NoDeliveryDateCount, Total = s.SalesOrders.NoInvoiceCount },
                new() { Code = ExecutiveDataQualityCalculator.SoNoInvoice, Count = s.SalesOrders.NoInvoiceCount, Total = s.SalesOrders.OpenCount, AmountThb = s.SalesOrders.NoInvoiceThb }
            };
        }

        // ผ่าน MaterialInsightService (cached path เดียวกับหน้าวัตถุดิบ/Digest: kind GOLD, 12 เดือน)
        private async Task<DataQualityInput> LoadNegativeGold()
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMaterialInsightService>();
            var res = await svc.Overview(new jewelry.Model.Material.Insight.Overview.Request { Kind = "GOLD", Months = MaterialMonths });

            return new DataQualityInput
            {
                Code = ExecutiveDataQualityCalculator.NegativeGold,
                Count = res.Kpi.NegativeCount,
                Total = res.Kpi.ItemCount,
                Params = new Dictionary<string, object>
                {
                    ["negativeWeight"] = res.Kpi.NegativeWeight,
                    ["inboundRecorded"] = res.Kpi.InboundRecorded
                }
            };
        }
    }
}
