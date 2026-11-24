using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Jewelry.Service.AiConnector.Pipeline
{
    // sliding window ต่อ user id (ไม่ใช่ IP/token) — เก็บในหน่วยความจำของ instance เดียว — default 30 ครั้ง/นาที
    public class AiRateLimiter
    {
        private readonly ConcurrentDictionary<long, Queue<DateTime>> _hits = new ConcurrentDictionary<long, Queue<DateTime>>();
        private readonly int _limit;
        private readonly Func<DateTime> _clock;

        public AiRateLimiter(IOptions<AiConnectorOptions> options) : this(options.Value.RateLimitPerMinute, () => DateTime.UtcNow) { }

        public AiRateLimiter(int limitPerMinute, Func<DateTime> clock)
        {
            _limit = limitPerMinute;
            _clock = clock;
        }

        public bool TryAcquire(long key)
        {
            var now = _clock();
            var q = _hits.GetOrAdd(key, _ => new Queue<DateTime>());
            lock (q)
            {
                while (q.Count > 0 && now - q.Peek() >= TimeSpan.FromMinutes(1)) q.Dequeue();
                if (q.Count >= _limit) return false;
                q.Enqueue(now);
                return true;
            }
        }
    }
}
