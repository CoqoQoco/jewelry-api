using Jewelry.Data.Context;
using Jewelry.Service.Production.Insight;
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
        private readonly IProductionWorkerLookupService _workerLookupService;

        private const int StatusDone = 100;
        private const int StatusMelted = 500;
        private const string LastMoveDatesCacheKey = "ProductionPlanWip:LastMoveDates";
        private static readonly TimeSpan LastMoveDatesCacheTtl = TimeSpan.FromMinutes(5);

        public ProductionPlanWipHelper(JewelryContext jewelryContext, IMemoryCache cache, IProductionWorkerLookupService workerLookupService)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
            _workerLookupService = workerLookupService;
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
        //
        // workerItems (รอบแก้ไข): รวมรหัสช่างจาก 3 แหล่ง (header.worker_code, detail.worker, detail.worker_sub)
        // dedupe ด้วยรหัส แล้ว resolve ชื่อผ่าน IProductionWorkerLookupService (tbm_worker.name_th) — ถ้าไม่เจอใน
        // master แต่รหัสตรงกับ header.worker_code ใช้ header.worker_name แทน ถ้ายังไม่เจออีกใช้รหัสเป็นชื่อ —
        // ช่าง TEST (code/name มีคำว่า TEST) ตัดทิ้งทั้งหมด ไม่โผล่เลย — placeholder "รอจ่าย..." ติด isQueue=true
        // แต่ยังแสดง (ไม่ตัดทิ้ง) — เรียงช่างจริงก่อนคิว — Workers (string[]) เดิม = ชื่อเฉพาะที่ไม่ใช่คิว (backward compat)
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
                    h.WorkerCode,
                    h.WorkerName
                })
                .ToListAsync();

            var latestHeaderByPlan = headerRows
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(h => h.CreateDate).First());

            var latestHeaderIds = latestHeaderByPlan.Values.Select(h => h.Id).ToList();

            var detailCodesByHeaderId = new Dictionary<int, List<string>>();
            if (latestHeaderIds.Count > 0)
            {
                var detailRows = await _jewelryContext.TbtProductionPlanStatusDetail
                    .AsNoTracking()
                    .Where(d => latestHeaderIds.Contains(d.HeaderId) && (!string.IsNullOrEmpty(d.Worker) || !string.IsNullOrEmpty(d.WorkerSub)))
                    .Select(d => new { d.HeaderId, d.ItemNo, d.Worker, d.WorkerSub })
                    .ToListAsync();

                detailCodesByHeaderId = detailRows
                    .OrderBy(d => d.ItemNo)
                    .GroupBy(d => d.HeaderId)
                    .ToDictionary(
                        g => g.Key,
                        g => g
                            .SelectMany(x => new[] { x.Worker, x.WorkerSub })
                            .Where(w => !string.IsNullOrWhiteSpace(w))
                            .Select(w => w!.Trim())
                            .Distinct()
                            .ToList());
            }

            var statusNames = await GetStatusNamesAsync();
            var workerNames = await _workerLookupService.GetWorkerNamesAsync();

            foreach (var plan in plans)
            {
                if (latestHeaderByPlan.TryGetValue(plan.Id, out var header))
                {
                    var headerWorkerCode = string.IsNullOrWhiteSpace(header.WorkerCode) ? null : header.WorkerCode.Trim();

                    var codes = new List<string>();
                    if (headerWorkerCode != null) codes.Add(headerWorkerCode);
                    if (detailCodesByHeaderId.TryGetValue(header.Id, out var detailCodes))
                    {
                        foreach (var c in detailCodes)
                        {
                            if (!codes.Contains(c)) codes.Add(c);
                        }
                    }

                    var workerItems = new List<PlanWorkerItem>();
                    foreach (var code in codes)
                    {
                        if (ProductionGoldStageEvaluator.IsTestWorker(code, workerNames)) continue;

                        string name;
                        if (workerNames.TryGetValue(code, out var nameTh) && !string.IsNullOrEmpty(nameTh))
                        {
                            name = nameTh;
                        }
                        else if (headerWorkerCode != null && code == headerWorkerCode && !string.IsNullOrWhiteSpace(header.WorkerName))
                        {
                            name = header.WorkerName.Trim();
                        }
                        else
                        {
                            name = code;
                        }

                        workerItems.Add(new PlanWorkerItem
                        {
                            Code = code,
                            Name = name,
                            IsQueue = ProductionGoldStageEvaluator.IsPlaceholderWorker(code, workerNames)
                        });
                    }

                    workerItems = workerItems.OrderBy(w => w.IsQueue ? 1 : 0).Take(5).ToList();

                    result[plan.Id] = new PlanLastActionInfo
                    {
                        LastUpdateBy = header.UpdateBy ?? header.CreateBy,
                        LastAction = statusNames.TryGetValue(header.Status, out var statusName) ? statusName : null,
                        LastActionRemark = header.Remark1,
                        LastActionDate = header.UpdateDate ?? header.CreateDate,
                        WorkerItems = workerItems,
                        Workers = workerItems.Where(w => !w.IsQueue).Select(w => w.Name).ToList()
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
                        WorkerItems = new List<PlanWorkerItem>(),
                        Workers = new List<string>()
                    };
                }
            }

            return result;
        }
    }
}
