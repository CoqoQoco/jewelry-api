using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.Common
{
    // cache ผลลัพธ์ insight (response DTO ที่คำนวณเสร็จแล้ว) — ใช้แบบ read-only เท่านั้น ห้าม mutate ผลที่ได้
    // single-flight ต่อ key: caller พร้อมกันรอ factory ตัวเดียว, factory ที่ throw ไม่ถูก cache,
    // Invalidate(prefix) ล้างทั้ง entry และ factory ที่กำลังรันอยู่ (ผลที่เริ่มคำนวณก่อน invalidate จะไม่ถูกเก็บ)
    public class InsightCache
    {
        public static readonly TimeSpan OverviewTtl = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan SummaryTtl = TimeSpan.FromSeconds(60);

        private readonly IMemoryCache _cache;
        private readonly ConcurrentDictionary<string, byte> _keys = new ConcurrentDictionary<string, byte>();
        private readonly ConcurrentDictionary<string, Lazy<Task<object>>> _inflight = new ConcurrentDictionary<string, Lazy<Task<object>>>();
        private long _generation;

        public InsightCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        public static string Key(string endpoint, params object?[] parts)
            => endpoint + ":" + string.Join(":", parts.Select(p => p?.ToString() ?? string.Empty));

        // มี override (what-if) อย่างน้อย 1 field ที่ไม่ใช่ null หรือไม่
        public static bool HasOverride(object? overrides)
        {
            if (overrides == null) return false;
            return overrides.GetType().GetProperties().Any(p => p.CanRead && p.GetIndexParameters().Length == 0 && p.GetValue(overrides) != null);
        }

        // request ที่มี override ข้าม cache ทั้งหมด (ไม่อ่านและไม่เขียน)
        public Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, object? overrides, Func<Task<T>> factory) where T : class
            => HasOverride(overrides) ? factory() : GetOrCreateAsync(key, ttl, factory);

        public async Task<T> GetOrCreateAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory) where T : class
        {
            if (_cache.TryGetValue(key, out var hit) && hit is T cached)
            {
                return cached;
            }

            var lazy = _inflight.GetOrAdd(key, k => new Lazy<Task<object>>(() => RunAsync(k, ttl, factory)));
            try
            {
                return (T)await lazy.Value;
            }
            finally
            {
                _inflight.TryRemove(new KeyValuePair<string, Lazy<Task<object>>>(key, lazy));
            }
        }

        private async Task<object> RunAsync<T>(string key, TimeSpan ttl, Func<Task<T>> factory) where T : class
        {
            if (_cache.TryGetValue(key, out var hit) && hit is T cached)
            {
                return cached;
            }

            var generation = Interlocked.Read(ref _generation);
            var value = await factory();

            if (value != null && generation == Interlocked.Read(ref _generation))
            {
                _keys[key] = 0;
                _cache.Set(key, value, new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(ttl)
                    .RegisterPostEvictionCallback((k, _, _, _) => _keys.TryRemove((string)k, out _)));
            }

            return value!;
        }

        public void Invalidate(string prefix)
        {
            Interlocked.Increment(ref _generation);

            foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }

            foreach (var key in _inflight.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _inflight.TryRemove(key, out _);
            }
        }
    }
}
