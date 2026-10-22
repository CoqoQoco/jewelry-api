using jewelry.Model.Material.Insight;
using Jewelry.Data.Context;
using Jewelry.Service.Common;
using Jewelry.Service.Material.Insight;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Min = jewelry.Model.Material.Insight.Min;
using Overview = jewelry.Model.Material.Insight.Overview;
using Policy = jewelry.Model.Material.Insight.Policy;

namespace Jewelry.Service.Tests.Material.Insight;

public class MaterialInsightCacheTests
{
    private class FakeProvider : IMaterialInsightProvider
    {
        public int Loads;
        public string Kind => "GOLD";
        public string Unit => "g";

        public Task<MaterialDataset> LoadAsync()
        {
            Interlocked.Increment(ref Loads);
            return Task.FromResult(new MaterialDataset
            {
                Items = new List<MaterialItemInput>
                {
                    new() { ItemKey = "A|1", Code = "A", SizeCode = "1", OnHand = 10, FirstSeenUtc = DateTime.UtcNow.AddDays(-100) }
                }
            });
        }
    }

    private class NoHttpContext : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static (MaterialInsightService Service, FakeProvider Provider) Create()
    {
        var options = new DbContextOptionsBuilder<JewelryContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var provider = new FakeProvider();
        var cache = new InsightCache(new MemoryCache(Options.Create(new MemoryCacheOptions())));
        var service = new MaterialInsightService(new JewelryContext(options), new NoHttpContext(), new[] { provider }, cache);
        return (service, provider);
    }

    private static Overview.Request Req(int months = 12, PolicyOverrides? thresholds = null)
        => new() { Kind = "GOLD", Months = months, Thresholds = thresholds };

    [Fact]
    public async Task OverviewIsCachedPerKindAndMonths()
    {
        var (service, provider) = Create();

        var a = await service.Overview(Req());
        var b = await service.Overview(Req());
        Assert.Same(a, b);
        Assert.Equal(1, provider.Loads);

        await service.Overview(Req(6));
        Assert.Equal(2, provider.Loads);
    }

    [Fact]
    public async Task ThresholdOverrideBypassesCache()
    {
        var (service, provider) = Create();

        await service.Overview(Req());
        await service.Overview(Req(thresholds: new PolicyOverrides { SafetyDays = 99 }));
        await service.Overview(Req());

        Assert.Equal(2, provider.Loads);
    }

    [Fact]
    public async Task SaveMinInvalidatesCachedOverview()
    {
        var (service, provider) = Create();

        var before = await service.Overview(Req());
        Assert.Equal(1, provider.Loads);

        await service.SaveMin(new Min.SaveRequest { Kind = "GOLD", ItemKey = "A|1", MinQty = 77 });

        var after = await service.Overview(Req());
        Assert.Equal(2, provider.Loads);
        Assert.NotSame(before, after);
        Assert.Equal(77, after.Items.Single().EffectiveMin);
    }

    [Fact]
    public async Task DeleteMinInvalidatesCachedOverview()
    {
        var (service, provider) = Create();
        await service.SaveMin(new Min.SaveRequest { Kind = "GOLD", ItemKey = "A|1", MinQty = 77 });
        var withMin = await service.Overview(Req());
        Assert.Equal(77, withMin.Items.Single().EffectiveMin);

        await service.DeleteMin(new Min.DeleteRequest { Kind = "GOLD", ItemKey = "A|1" });

        var after = await service.Overview(Req());
        Assert.NotEqual(77, after.Items.Single().EffectiveMin);
        Assert.Equal(2, provider.Loads);
    }

    [Fact]
    public async Task SavePolicyInvalidatesCachedOverview()
    {
        var (service, provider) = Create();

        var before = await service.Overview(Req());
        await service.SavePolicy(new Policy.SaveRequest { Kind = "GOLD", SafetyDays = 30 });
        var after = await service.Overview(Req());

        Assert.Equal(2, provider.Loads);
        Assert.Equal(30, after.Policy.SafetyDays);
        Assert.NotSame(before, after);
    }

    [Fact]
    public async Task FailedSaveDoesNotInvalidate()
    {
        var (service, provider) = Create();
        await service.Overview(Req());

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveMin(new Min.SaveRequest { Kind = "GOLD", ItemKey = "A|1", MinQty = -1 }));
        await service.Overview(Req());

        Assert.Equal(1, provider.Loads);
    }
}
