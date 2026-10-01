using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    // ตาราง tbt_production_delivery_target เป็น append-only history (ค่าเดียวทั้งระบบ ไม่แยกต่อแผนก) — โหลด
    // ทั้งตาราง (เล็กมาก) แล้วเลือก "ล่าสุด" ในหน่วยความจำ (ห้าม grouped.First() แปลเป็น SQL) — cache 5 นาที,
    // invalidate ทันทีหลัง SaveAsync
    public class ProductionDeliveryTargetService : IProductionDeliveryTargetService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const string CurrentCacheKey = "ProductionInsight:DeliveryTarget:Current";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionDeliveryTargetService(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        public async Task<DeliveryTargetRow?> GetCurrentTargetAsync()
        {
            if (_cache.TryGetValue<DeliveryTargetRow?>(CurrentCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbtProductionDeliveryTarget
                .AsNoTracking()
                .Select(t => new DeliveryTargetRow
                {
                    TargetPercent = t.TargetPercent,
                    EffectiveFrom = t.EffectiveFrom,
                    CreateBy = t.CreateBy,
                    Remark = t.Remark
                })
                .ToListAsync();

            var result = rows.OrderByDescending(r => r.EffectiveFrom).FirstOrDefault();
            if (result != null)
            {
                _cache.Set(CurrentCacheKey, result, CacheTtl);
            }

            return result;
        }

        public async Task<List<DeliveryTargetRow>> GetHistoryAsync()
        {
            return await _jewelryContext.TbtProductionDeliveryTarget
                .AsNoTracking()
                .OrderByDescending(t => t.EffectiveFrom)
                .Select(t => new DeliveryTargetRow
                {
                    TargetPercent = t.TargetPercent,
                    EffectiveFrom = t.EffectiveFrom,
                    CreateBy = t.CreateBy,
                    Remark = t.Remark
                })
                .ToListAsync();
        }

        public async Task SaveAsync(decimal targetPercent, string? remark, string createBy)
        {
            var now = DateTime.UtcNow;

            var row = new TbtProductionDeliveryTarget
            {
                TargetPercent = targetPercent,
                EffectiveFrom = now,
                Remark = remark,
                CreateDate = now,
                CreateBy = createBy
            };

            await _jewelryContext.TbtProductionDeliveryTarget.AddAsync(row);
            await _jewelryContext.SaveChangesAsync();

            _cache.Remove(CurrentCacheKey);
        }
    }
}
