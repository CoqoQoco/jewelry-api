using Jewelry.Service.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jewelry.Service.Tests.Common;

public class InsightCacheTests
{
    private class Dto
    {
        public int N { get; set; }
    }

    private class Overrides
    {
        public int? A { get; set; }
        public decimal? B { get; set; }
    }

    private static InsightCache Create() => new(new MemoryCache(Options.Create(new MemoryCacheOptions())));

    private static readonly TimeSpan Long = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task HitReturnsSameInstanceWithoutCallingFactory()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory() { calls++; return Task.FromResult(new Dto { N = calls }); }

        var first = await cache.GetOrCreateAsync("k", Long, Factory);
        var second = await cache.GetOrCreateAsync("k", Long, Factory);

        Assert.Equal(1, calls);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task DifferentKeysAreIndependent()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory() { calls++; return Task.FromResult(new Dto { N = calls }); }

        await cache.GetOrCreateAsync("a", Long, Factory);
        await cache.GetOrCreateAsync("b", Long, Factory);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ExpiredEntryIsRecomputed()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory() { calls++; return Task.FromResult(new Dto { N = calls }); }

        await cache.GetOrCreateAsync("k", TimeSpan.FromMilliseconds(40), Factory);
        await Task.Delay(150);
        var again = await cache.GetOrCreateAsync("k", TimeSpan.FromMilliseconds(40), Factory);

        Assert.Equal(2, calls);
        Assert.Equal(2, again.N);
    }

    [Fact]
    public async Task OverrideBypassesCacheBothWays()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory() { calls++; return Task.FromResult(new Dto { N = calls }); }

        await cache.GetOrCreateAsync("k", Long, new Overrides { A = 1 }, Factory);
        await cache.GetOrCreateAsync("k", Long, new Overrides { B = 2m }, Factory);
        Assert.Equal(2, calls);

        // override ไม่เขียน cache: request ปกติถัดมาต้องคำนวณเอง แล้วจึงถูกเก็บ
        await cache.GetOrCreateAsync("k", Long, (Overrides?)null, Factory);
        await cache.GetOrCreateAsync("k", Long, new Overrides(), Factory);
        Assert.Equal(3, calls);

        // override ไม่อ่าน cache: แม้มี entry อยู่แล้วก็ต้องคำนวณใหม่
        await cache.GetOrCreateAsync("k", Long, new Overrides { A = 5 }, Factory);
        Assert.Equal(4, calls);
    }

    [Fact]
    public void HasOverrideDetectsAnyNonNullField()
    {
        Assert.False(InsightCache.HasOverride(null));
        Assert.False(InsightCache.HasOverride(new Overrides()));
        Assert.True(InsightCache.HasOverride(new Overrides { B = 0m }));
    }

    [Fact]
    public async Task InvalidateRemovesOnlyMatchingPrefix()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory() { calls++; return Task.FromResult(new Dto { N = calls }); }

        await cache.GetOrCreateAsync("Material:GOLD:12", Long, Factory);
        await cache.GetOrCreateAsync("Material:GOLD:6", Long, Factory);
        await cache.GetOrCreateAsync("Material:GEM:12", Long, Factory);
        Assert.Equal(3, calls);

        cache.Invalidate("Material:GOLD:");

        await cache.GetOrCreateAsync("Material:GOLD:12", Long, Factory);
        await cache.GetOrCreateAsync("Material:GOLD:6", Long, Factory);
        Assert.Equal(5, calls);

        await cache.GetOrCreateAsync("Material:GEM:12", Long, Factory);
        Assert.Equal(5, calls);
    }

    [Fact]
    public async Task FailureIsNotCachedAndNextCallRetries()
    {
        var cache = Create();
        var calls = 0;
        Task<Dto> Factory()
        {
            calls++;
            if (calls == 1) throw new InvalidOperationException("boom");
            return Task.FromResult(new Dto { N = calls });
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync("k", Long, Factory));
        var ok = await cache.GetOrCreateAsync("k", Long, Factory);

        Assert.Equal(2, calls);
        Assert.Equal(2, ok.N);
    }

    [Fact]
    public async Task AsyncFailureIsNotCached()
    {
        var cache = Create();
        var calls = 0;
        async Task<Dto> Factory()
        {
            calls++;
            await Task.Yield();
            if (calls == 1) throw new InvalidOperationException("boom");
            return new Dto { N = calls };
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync("k", Long, Factory));
        await cache.GetOrCreateAsync("k", Long, Factory);
        await cache.GetOrCreateAsync("k", Long, Factory);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task ConcurrentCallersShareOneFactoryCall()
    {
        var cache = Create();
        var calls = 0;
        var gate = new TaskCompletionSource();
        async Task<Dto> Factory()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return new Dto { N = 1 };
        }

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() => cache.GetOrCreateAsync("k", Long, Factory))).ToArray();
        await Task.Delay(100);
        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, calls);
        Assert.All(results, r => Assert.Same(results[0], r));
    }

    [Fact]
    public async Task ResultStartedBeforeInvalidateIsNotStored()
    {
        var cache = Create();
        var calls = 0;
        var started = new TaskCompletionSource();
        var gate = new TaskCompletionSource();
        async Task<Dto> Slow()
        {
            Interlocked.Increment(ref calls);
            started.SetResult();
            await gate.Task;
            return new Dto { N = 1 };
        }

        var pending = cache.GetOrCreateAsync("P:k", Long, Slow);
        await started.Task;
        cache.Invalidate("P:");
        gate.SetResult();
        await pending;

        var fresh = await cache.GetOrCreateAsync("P:k", Long, () => Task.FromResult(new Dto { N = 2 }));
        Assert.Equal(2, fresh.N);
    }

    [Fact]
    public void KeyJoinsEndpointAndParts()
    {
        Assert.Equal("E:GOLD:12", InsightCache.Key("E", "GOLD", 12));
    }
}
