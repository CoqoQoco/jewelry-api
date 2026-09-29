using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Production.Shared
{
    // Shared ระหว่าง ExecutiveReportService (Report/Executive) และ ProductionInsightService (Production/Insight)
    // ย้ายมาจาก ExecutiveReportService เดิม — พฤติกรรม/ผลลัพธ์ต้องเหมือนเดิมทุกประการ (แค่เปลี่ยน cache key)
    public class ProductionPlanWipHelper : IProductionPlanWipHelper
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        private const int StatusDone = 100;
        private const int StatusMelted = 500;
        private const string LastMoveDatesCacheKey = "ProductionPlanWip:LastMoveDates";
        private static readonly TimeSpan LastMoveDatesCacheTtl = TimeSpan.FromMinutes(5);

        public ProductionPlanWipHelper(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
        }

        // เฉพาะ plan ที่ active และยังไม่จบ/ไม่ถูกหลอม (status นอก {100,500})
        public async Task<List<OpenPlanRow>> GetOpenPlansAsync()
        {
            return await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .Where(p => p.IsActive == true && p.Status != StatusDone && p.Status != StatusMelted)
                .Select(p => new OpenPlanRow
                {
                    Id = p.Id,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    WoText = p.WoText,
                    Mold = p.Mold,
                    ProductNumber = p.ProductNumber,
                    ProductName = p.ProductName,
                    ProductQty = p.ProductQty,
                    Status = p.Status,
                    RequestDate = p.RequestDate,
                    CreateDate = p.CreateDate,
                    CreateBy = p.CreateBy,
                    UpdateDate = p.UpdateDate,
                    UpdateBy = p.UpdateBy
                })
                .ToListAsync();
        }

        public async Task<int> GetMeltedOpenCountAsync()
        {
            return await _jewelryContext.TbtProductionPlan
                .AsNoTracking()
                .CountAsync(p => p.IsActive == true && p.Status == StatusMelted);
        }

        // Max(create_date) ของ tbt_production_plan_status_header ต่อ production_plan_id
        // GroupBy + Max เท่านั้น (ห้าม First() ในนี้ — เคยทำ CPU DB พุ่งมาแล้ว)
        // cache 5 นาที — ใช้ร่วมกันระหว่าง ExecutiveReport/ProductionInsight ไม่ต้อง scan ~52k rows ซ้ำหลายรอบ
        public async Task<Dictionary<int, DateTime>> GetLastMoveDatesAsync()
        {
            if (_cache.TryGetValue<Dictionary<int, DateTime>>(LastMoveDatesCacheKey, out var cached) && cached != null)
            {
                return cached;
            }

            var rows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .GroupBy(h => h.ProductionPlanId)
                .Select(g => new { PlanId = g.Key, LastDate = g.Max(x => x.CreateDate) })
                .ToListAsync();

            var result = rows.ToDictionary(x => x.PlanId, x => x.LastDate);
            _cache.Set(LastMoveDatesCacheKey, result, LastMoveDatesCacheTtl);
            return result;
        }

        public async Task<Dictionary<int, string>> GetStatusNamesAsync()
        {
            return await _jewelryContext.TbmProductionPlanStatus
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x.NameTh);
        }

        // "ทำอะไรล่าสุด" ต่อแผน — enrich เฉพาะหน้าปัจจุบันหลัง paging เท่านั้น (ชุดข้อมูลเล็กเสมอ ไม่เกิน take)
        // เลือก header ล่าสุดต่อแผน "ในหน่วยความจำ" หลัง ToListAsync() แล้วเท่านั้น — ห้าม grouped.First() ที่ EF แปลเป็น SQL
        public async Task<Dictionary<int, PlanLastActionInfo>> GetLastActionInfoAsync(IReadOnlyList<OpenPlanRow> plans)
        {
            var result = new Dictionary<int, PlanLastActionInfo>();
            if (plans == null || plans.Count == 0) return result;

            var planIds = plans.Select(p => p.Id).ToList();

            var headerRows = await _jewelryContext.TbtProductionPlanStatusHeader
                .AsNoTracking()
                .Where(h => planIds.Contains(h.ProductionPlanId) && h.IsActive == true)
                .Select(h => new
                {
                    h.Id,
                    h.ProductionPlanId,
                    h.Status,
                    h.CreateDate,
                    h.UpdateDate,
                    h.CreateBy,
                    h.UpdateBy,
                    h.Remark1,
                    h.WorkerName
                })
                .ToListAsync();

            var latestHeaderByPlan = headerRows
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.CreateDate).First());

            var latestHeaderIds = latestHeaderByPlan.Values.Select(h => h.Id).ToList();

            var workersByHeaderId = new Dictionary<int, List<string>>();
            if (latestHeaderIds.Count > 0)
            {
                var detailRows = await _jewelryContext.TbtProductionPlanStatusDetail
                    .AsNoTracking()
                    .Where(d => latestHeaderIds.Contains(d.HeaderId) && !string.IsNullOrEmpty(d.Worker))
                    .Select(d => new { d.HeaderId, d.ItemNo, d.Worker })
                    .ToListAsync();

                workersByHeaderId = detailRows
                    .OrderBy(d => d.ItemNo)
                    .GroupBy(d => d.HeaderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.Worker!.Trim()).Where(w => w.Length > 0).Distinct().ToList());
            }

            var statusNames = await GetStatusNamesAsync();

            foreach (var plan in plans)
            {
                if (latestHeaderByPlan.TryGetValue(plan.Id, out var header))
                {
                    var workers = new List<string>();
                    if (!string.IsNullOrWhiteSpace(header.WorkerName))
                    {
                        workers.Add(header.WorkerName.Trim());
                    }

                    if (workersByHeaderId.TryGetValue(header.Id, out var detailWorkers))
                    {
                        foreach (var w in detailWorkers)
                        {
                            if (!workers.Contains(w)) workers.Add(w);
                        }
                    }

                    result[plan.Id] = new PlanLastActionInfo
                    {
                        LastUpdateBy = header.UpdateBy ?? header.CreateBy,
                        LastAction = statusNames.TryGetValue(header.Status, out var statusName) ? statusName : null,
                        LastActionRemark = header.Remark1,
                        LastActionDate = header.UpdateDate ?? header.CreateDate,
                        Workers = workers.Take(5).ToList()
                    };
                }
                else
                {
                    result[plan.Id] = new PlanLastActionInfo
                    {
                        LastUpdateBy = plan.UpdateBy ?? plan.CreateBy,
                        LastAction = null,
                        LastActionRemark = null,
                        LastActionDate = plan.UpdateDate ?? plan.CreateDate,
                        Workers = new List<string>()
                    };
                }
            }

            return result;
        }
    }
}
