using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Insight
{
    // โหลดข้อมูลเสริมเฉพาะของ ProductionInsight/Delivery (ส่งงานตรงเวลา) — reuse ProductionPlanTrendDataProvider
    // สำหรับชุด plan หลัก (ไม่ query ตาราง tbt_production_plan ซ้ำ) แล้วเติมเฉพาะส่วนที่ trend data ไม่มี:
    // - DoneDate ต่อแผน = transfer แรกเข้า status 100 (tbt_production_plan_transfer_status) หรือ fallback
    //   completed_date ถ้าไม่มี row transfer เลย (ดู FACTS ในสเปก — งานเก่าก่อนมีตารางนี้)
    // - ชื่อลูกค้า (tbm_customer) สำหรับ lateCustomers aggregation
    // cache 5 นาที เหมือน provider อื่นๆ ในโมดูลนี้
    public class ProductionDeliveryDataProvider : IProductionDeliveryDataProvider
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IProductionPlanTrendDataProvider _trendDataProvider;
        private readonly IMemoryCache _cache;

        private const int StatusCompleted = 100;
        private const string DoneDatesCacheKey = "ProductionInsight:Delivery:DoneDates";
        private const string CustomerNamesCacheKey = "ProductionInsight:Delivery:CustomerNames";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

        public ProductionDeliveryDataProvider(
            JewelryContext jewelryContext,
            IProductionPlanTrendDataProvider trendDataProvider,
            IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _trendDataProvider = trendDataProvider;
            _cache = cache;
        }

        public async Task<Dictionary<int, DateTime>> GetDoneDatesAsync()
        {
            if (_cache.TryGetValue<Dictionary<int, DateTime>>(DoneDatesCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var trendData = await _trendDataProvider.GetTrendDataAsync();

            // MIN(create_date) ต่อแผน ที่ target_status = 100 (transfer แรกเข้า "เสร็จ") — GroupBy+Min เท่านั้น
            // (ห้าม grouped.First() แปลเป็น correlated subquery — เคยทำ CPU DB พุ่งมาแล้ว)
            var firstDoneTransfers = await _jewelryContext.TbtProductionPlanTransferStatus
                .AsNoTracking()
                .Where(t => t.TargetStatus == StatusCompleted)
                .GroupBy(t => t.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, DoneDate = g.Min(x => x.CreateDate) })
                .ToListAsync();

            var result = firstDoneTransfers.ToDictionary(x => x.PlanId, x => x.DoneDate);

            // fallback completed_date สำหรับแผนที่ไม่มี row transfer เลย (ข้อมูลเก่าก่อนมีตารางนี้)
            foreach (var plan in trendData.Plans)
            {
                if (!result.ContainsKey(plan.Id) && plan.CompletedDate.HasValue)
                {
                    result[plan.Id] = plan.CompletedDate.Value;
                }
            }

            _cache.Set(DoneDatesCacheKey, result, CacheTtl);
            return result;
        }

        public async Task<Dictionary<string, string>> GetCustomerNamesAsync()
        {
            if (_cache.TryGetValue<Dictionary<string, string>>(CustomerNamesCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var result = await _jewelryContext.TbmCustomer
                .AsNoTracking()
                .ToDictionaryAsync(c => c.Code, c => c.NameTh);

            _cache.Set(CustomerNamesCacheKey, result, CacheTtl);
            return result;
        }
    }
}
