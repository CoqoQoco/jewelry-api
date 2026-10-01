using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    // tbm_worker เป็นตาราง master เล็ก (โหลดทั้งตารางได้สบาย) — cache 10 นาทีตามสั่ง (นานกว่า provider อื่นใน
    // โมดูลนี้ที่ใช้ 5 นาที เพราะ master นี้แทบไม่เปลี่ยน)
    public class ProductionWorkerLookupService : IProductionWorkerLookupService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string CacheKey = "ProductionInsight:WorkerNames";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

        public ProductionWorkerLookupService(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<Dictionary<string, string>> GetWorkerNamesAsync()
        {
            if (_cache.TryGetValue<Dictionary<string, string>>(CacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var result = await _jewelryContext.TbmWorker
                .AsNoTracking()
                .ToDictionaryAsync(w => w.Code, w => w.NameTh);

            _cache.Set(CacheKey, result, CacheTtl);
            return result;
        }
    }
}
