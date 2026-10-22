using Jewelry.Service.Common;
using Jewelry.Service.Helper;
using Jewelry.Service.Material.Insight;
using Jewelry.Service.Production.Insight;
using Jewelry.Service.Sale.Insight;
using Jewelry.Service.Stock.Insight;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Digest = jewelry.Model.Report.Executive.Digest;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;

namespace Jewelry.Service.Report.Executive
{
    public class ExecutiveDigestService : IExecutiveDigestService
    {
        // Digest key อยู่ใต้ prefix ของ MaterialInsight (GOLD) เพื่อให้ SavePolicy/SaveMin/DeleteMin ที่ Invalidate(prefix) ล้างด้วย
        public static string CacheKey(string metal) => MaterialInsightService.CachePrefix("GOLD") + "Digest:" + metal;

        public static readonly TimeSpan DigestTtl = TimeSpan.FromSeconds(60);

        private const int OverviewMonths = 12;
        private const int ProductionRangeMonths = 3;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly InsightCache _insightCache;
        private readonly ILogger<ExecutiveDigestService> _logger;

        public ExecutiveDigestService(IServiceScopeFactory scopeFactory, InsightCache insightCache, ILogger<ExecutiveDigestService> logger)
        {
            _scopeFactory = scopeFactory;
            _insightCache = insightCache;
            _logger = logger;
        }

        // Digest ที่มี domain ใช้ไม่ได้ ห้ามเข้า cache — ส่งผลออกทาง exception (InsightCache ไม่ cache factory ที่ throw)
        private class IncompleteDigestException : Exception
        {
            public IncompleteDigestException(Digest.Response response) { Response = response; }
            public Digest.Response Response { get; }
        }

        public async Task<Digest.Response> Digest(Digest.Request request)
        {
            var metal = MetalClassifier.Normalize(request?.Metal);
            try
            {
                return await _insightCache.GetOrCreateAsync(CacheKey(metal), DigestTtl, async () =>
                {
                    var response = await BuildAsync(metal);
                    if (response.Domains.Any(d => !d.Available)) throw new IncompleteDigestException(response);
                    return response;
                });
            }
            catch (IncompleteDigestException ex)
            {
                return ex.Response;
            }
        }

        private async Task<Digest.Response> BuildAsync(string metal)
        {
            var inputs = await Task.WhenAll(
                Safe("production", () => LoadProduction(metal)),
                Safe("sales", () => LoadSales(metal)),
                Safe("material", LoadMaterial),
                Safe("stock", () => LoadStock(metal)));
            return ExecutiveDigestRanker.Build(DateTime.UtcNow, metal, inputs);
        }

        private async Task<DigestDomainInput> Safe(string key, Func<Task<DigestDomainInput>> load)
        {
            try
            {
                return await load();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExecutiveDigest: domain {Domain} failed", key);
                return new DigestDomainInput { Key = key, Available = false };
            }
        }

        // แต่ละ domain/topic ใช้ DI scope ของตัวเอง — JewelryContext ไม่ thread-safe ห้ามแชร์ข้าม task ที่รันขนาน
        private async Task<DigestDomainInput> LoadStock(string metal)
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IStockInsightService>();
            var res = await svc.Overview(new jewelry.Model.Stock.Insight.Overview.Request { Metal = metal, Months = OverviewMonths });
            return From("stock", "stock", res.Problems, res.Forecasts);
        }

        private async Task<DigestDomainInput> LoadSales(string metal)
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ISalesInsightService>();
            var res = await svc.Overview(new jewelry.Model.Sale.Insight.Overview.Request { Metal = metal, Months = OverviewMonths });
            return From("sales", "sales", res.Problems, res.Forecasts);
        }

        private async Task<DigestDomainInput> LoadMaterial()
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMaterialInsightService>();
            var res = await svc.Overview(new jewelry.Model.Material.Insight.Overview.Request { Kind = "GOLD", Months = OverviewMonths });
            return From("material", "material", res.Problems, res.Forecasts);
        }

        // ไม่มี endpoint รวม — เรียก 6 topic ขนาน (ช่วง 3 เดือนล่าสุดแบบเวลาไทย เหมือน default ของหน้า) แล้วรวม; topic ไหน throw = production ใช้ไม่ได้
        private async Task<DigestDomainInput> LoadProduction(string metal)
        {
            var end = DateTimeOffset.UtcNow;
            var thaiToday = end.ToOffset(TimeSpan.FromHours(7)).Date;
            var start = new DateTimeOffset(thaiToday.AddMonths(-ProductionRangeMonths), TimeSpan.FromHours(7));
            var goldMetal = metal == MetalClassifier.Silver ? MetalClassifier.Silver : MetalClassifier.Gold;

            var topics = await Task.WhenAll(
                Topic("wip", async s => { var r = await s.Wip(new jewelry.Model.Production.Insight.Wip.Request { Metal = metal }); return (r.Problems, r.Forecasts); }),
                Topic("delivery", async s => { var r = await s.Delivery(new jewelry.Model.Production.Insight.Delivery.Request { Metal = metal, Start = start, End = end }); return (r.Problems, r.Forecasts); }),
                Topic("capacity", async s => { var r = await s.Capacity(new jewelry.Model.Production.Insight.Capacity.Request { Metal = metal, Start = start, End = end }); return (r.Problems, r.Forecasts); }),
                Topic("workers", async s => { var r = await s.Workers(new jewelry.Model.Production.Insight.Workers.Request { Metal = metal, Start = start, End = end }); return (r.Problems, r.Forecasts); }),
                Topic("gold", async s => { var r = await s.Gold(new jewelry.Model.Production.Insight.Gold.Request { Metal = goldMetal, Start = start, End = end }); return (r.Problems, r.Forecasts); }),
                Topic("materials", async s => { var r = await s.Materials(new jewelry.Model.Production.Insight.Materials.Request { Metal = metal, Start = start, End = end }); return (r.Problems, r.Forecasts); }));

            return Merge("production", topics);
        }

        private async Task<(string Tab, List<Finding> Problems, List<Finding> Forecasts)> Topic(
            string tab, Func<IProductionInsightService, Task<(List<Finding> Problems, List<Finding> Forecasts)>> call)
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IProductionInsightService>();
            var (problems, forecasts) = await call(svc);
            return (tab, problems, forecasts);
        }

        public static DigestDomainInput Merge(string domain, IEnumerable<(string Tab, List<Finding> Problems, List<Finding> Forecasts)> topics)
        {
            var input = new DigestDomainInput { Key = domain, Available = true };
            foreach (var t in topics)
            {
                input.Problems.AddRange(t.Problems.Select(p => new DigestTabFinding(p, t.Tab)));
                input.ForecastCount += t.Forecasts.Count;
            }
            return input;
        }

        private static DigestDomainInput From(string domain, string tab, List<Finding> problems, List<Finding> forecasts)
            => Merge(domain, new[] { (tab, problems, forecasts) });
    }
}
