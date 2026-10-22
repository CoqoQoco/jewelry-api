using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using jewelry.Model.Material.Insight;
using Min = jewelry.Model.Material.Insight.Min;
using Overview = jewelry.Model.Material.Insight.Overview;
using Policy = jewelry.Model.Material.Insight.Policy;

namespace Jewelry.Service.Material.Insight
{
    public class MaterialInsightService : BaseService, IMaterialInsightService
    {
        private const int NoteMaxLength = 500;

        private readonly JewelryContext _jewelryContext;
        private readonly IEnumerable<IMaterialInsightProvider> _providers;
        private readonly InsightCache _insightCache;

        public MaterialInsightService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IEnumerable<IMaterialInsightProvider> providers, InsightCache insightCache)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _providers = providers;
            _insightCache = insightCache;
        }

        public const string CacheEndpoint = "MaterialInsight:Overview";

        public static string CachePrefix(string kind) => CacheEndpoint + ":" + kind + ":";

        public Task<Overview.Response> Overview(Overview.Request request)
        {
            var kind = GetProvider(request.Kind).Kind;
            var key = CachePrefix(kind) + MaterialInsightThresholds.ClampMonths(request.Months);
            return _insightCache.GetOrCreateAsync(key, InsightCache.OverviewTtl, request.Thresholds, () => OverviewCore(request));
        }

        private async Task<Overview.Response> OverviewCore(Overview.Request request)
        {
            var provider = GetProvider(request.Kind);
            var kind = provider.Kind;
            var months = MaterialInsightThresholds.ClampMonths(request.Months);
            var now = DateTime.UtcNow;

            var policy = await ResolvePolicyAsync(kind, request.Thresholds);
            var overrides = await _jewelryContext.TbmMaterialStockMin
                .AsNoTracking()
                .Where(x => x.MaterialKind == kind && x.IsActive)
                .Select(x => new { x.ItemKey, x.MinQty, x.LeadTimeDays })
                .ToListAsync();
            var overrideMap = overrides.ToDictionary(x => x.ItemKey, x => new MaterialMinOverride(x.MinQty, x.LeadTimeDays));

            var dataset = await provider.LoadAsync();

            // เก็บเฉพาะ movement ของ item ที่มีในคลัง เพื่อให้ยอดย้อนหลังตรงกับยอดรวมปัจจุบัน
            var keys = new HashSet<string>(dataset.Items.Select(i => i.ItemKey));
            var movements = dataset.Movements.Where(m => keys.Contains(m.ItemKey)).ToList();

            var items = MaterialInsightCalculator.BuildItems(kind, dataset.Items, movements, overrideMap, policy, now);

            var monthStarts = MaterialInsightCalculator.MonthStarts(now, months);
            var series = MaterialInsightCalculator.BuildSeries(monthStarts, movements, dataset.Items.Sum(i => i.OnHand));

            var firstConsumed = MaterialInsightCalculator.FirstConsumedUtc(movements);
            var startMonth = firstConsumed.HasValue ? MaterialInsightCalculator.ThaiMonthStart(firstConsumed.Value) : (DateTime?)null;
            var historyMonths = MaterialInsightCalculator.FullMonthsSince(startMonth, MaterialInsightCalculator.ThaiMonthStart(now));

            var kpi = MaterialInsightCalculator.BuildKpi(items, dataset.OnProcessAgeSupported, policy, dataset.InboundRecorded);

            var problems = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                MaterialInsightRuleEngine.EvaluateNegativeBalance(items, dataset.InboundRecorded),
                MaterialInsightRuleEngine.EvaluateZeroStock(items),
                MaterialInsightRuleEngine.EvaluateBelowMin(items),
                MaterialInsightRuleEngine.EvaluateNearMin(items),
                MaterialInsightRuleEngine.EvaluateDeadStock(items, policy),
                MaterialInsightRuleEngine.EvaluateOnProcessStale(items, dataset.OnProcessAgeSupported, policy)
            }.Where(f => f != null).Select(f => f!).ToList();

            var forecasts = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                MaterialInsightRuleEngine.EvaluateStockout(items, historyMonths, policy)
            }.Where(f => f != null).Select(f => f!).ToList();

            return new Overview.Response
            {
                AsOf = now,
                Kind = kind,
                Unit = provider.Unit,
                Status = MaterialInsightRuleEngine.WorstSeverity(problems.Concat(forecasts)),
                Policy = policy,
                UsageHistory = new Overview.UsageHistory
                {
                    StartMonth = startMonth?.ToString("yyyy-MM-dd"),
                    FullMonths = historyMonths
                },
                Kpi = kpi,
                Items = items,
                Groups = MaterialInsightCalculator.BuildGroups(items),
                Series = series,
                Problems = problems,
                Forecasts = forecasts,
                Actions = MaterialInsightRuleEngine.BuildActions(problems, forecasts)
            };
        }

        public async Task<Policy.Response> GetPolicy(Policy.GetRequest request)
        {
            var kind = GetProvider(request.Kind).Kind;
            return new Policy.Response { Kind = kind, Policy = await ResolvePolicyAsync(kind, null) };
        }

        public async Task<Policy.Response> SavePolicy(Policy.SaveRequest request)
        {
            var kind = GetProvider(request.Kind).Kind;
            var entity = await _jewelryContext.TbmMaterialStockPolicy.FirstOrDefaultAsync(x => x.MaterialKind == kind);

            var changes = new PolicyOverrides
            {
                WindowMonths = request.WindowMonths,
                LeadTimeDays = request.LeadTimeDays,
                SafetyDays = request.SafetyDays,
                FloorQty = request.FloorQty,
                NearPercent = request.NearPercent,
                DeadStockDays = request.DeadStockDays,
                ForecastMonths = request.ForecastMonths,
                OnProcessStaleDays = request.OnProcessStaleDays,
                PriceChangePercent = request.PriceChangePercent,
                ConcentrationPercent = request.ConcentrationPercent,
                MinHistoryMonths = request.MinHistoryMonths
            };

            // merge: ค่าที่ส่งมา > ค่าในตาราง > default แล้ว clamp — field ที่เป็น null คงค่าเดิม
            var resolved = MaterialInsightThresholds.Resolve(entity == null ? null : MaterialInsightThresholds.FromEntity(entity), changes);
            var user = CurrentUsername ?? "system";
            var now = DateTime.UtcNow;

            var isNew = entity == null;
            entity ??= new TbmMaterialStockPolicy { MaterialKind = kind, CreateBy = user, CreateDate = now };

            entity.WindowMonths = resolved.WindowMonths;
            entity.LeadTimeDays = resolved.LeadTimeDays;
            entity.SafetyDays = resolved.SafetyDays;
            entity.FloorQty = resolved.FloorQty;
            entity.NearPercent = resolved.NearPercent;
            entity.DeadStockDays = resolved.DeadStockDays;
            entity.ForecastMonths = resolved.ForecastMonths;
            entity.OnProcessStaleDays = resolved.OnProcessStaleDays;
            entity.PriceChangePercent = resolved.PriceChangePercent;
            entity.ConcentrationPercent = resolved.ConcentrationPercent;
            entity.MinHistoryMonths = resolved.MinHistoryMonths;

            if (isNew)
            {
                _jewelryContext.TbmMaterialStockPolicy.Add(entity);
            }
            else
            {
                entity.UpdateBy = user;
                entity.UpdateDate = now;
                _jewelryContext.TbmMaterialStockPolicy.Update(entity);
            }

            await _jewelryContext.SaveChangesAsync();
            _insightCache.Invalidate(CachePrefix(kind));

            resolved.Source = MaterialInsightThresholds.SourceDb;
            return new Policy.Response { Kind = kind, Policy = resolved };
        }

        public async Task<Min.Response> SaveMin(Min.SaveRequest request)
        {
            var kind = GetProvider(request.Kind).Kind;
            var itemKey = request.ItemKey?.Trim();
            if (string.IsNullOrEmpty(itemKey)) throw new ArgumentException("กรุณาระบุรหัสรายการ (itemKey)");
            if (request.MinQty < 0) throw new ArgumentException("ขั้นต่ำต้องไม่ติดลบ");

            var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            if (note != null && note.Length > NoteMaxLength) throw new ArgumentException($"หมายเหตุยาวเกิน {NoteMaxLength} ตัวอักษร");

            var leadTime = request.LeadTimeDays.HasValue ? MaterialInsightThresholds.ClampLeadTimeDays(request.LeadTimeDays.Value) : (int?)null;
            var user = CurrentUsername ?? "system";
            var now = DateTime.UtcNow;

            var entity = await _jewelryContext.TbmMaterialStockMin.FirstOrDefaultAsync(x => x.MaterialKind == kind && x.ItemKey == itemKey);
            if (entity == null)
            {
                entity = new TbmMaterialStockMin
                {
                    MaterialKind = kind,
                    ItemKey = itemKey,
                    MinQty = request.MinQty,
                    LeadTimeDays = leadTime,
                    Note = note,
                    IsActive = true,
                    CreateBy = user,
                    CreateDate = now
                };
                _jewelryContext.TbmMaterialStockMin.Add(entity);
            }
            else
            {
                entity.MinQty = request.MinQty;
                entity.LeadTimeDays = leadTime;
                entity.Note = note;
                entity.IsActive = true;
                entity.UpdateBy = user;
                entity.UpdateDate = now;
                _jewelryContext.TbmMaterialStockMin.Update(entity);
            }

            await _jewelryContext.SaveChangesAsync();
            _insightCache.Invalidate(CachePrefix(kind));
            return ToMinResponse(entity);
        }

        public async Task<Min.Response> DeleteMin(Min.DeleteRequest request)
        {
            var kind = GetProvider(request.Kind).Kind;
            var itemKey = request.ItemKey?.Trim();
            if (string.IsNullOrEmpty(itemKey)) throw new ArgumentException("กรุณาระบุรหัสรายการ (itemKey)");

            var entity = await _jewelryContext.TbmMaterialStockMin
                .FirstOrDefaultAsync(x => x.MaterialKind == kind && x.ItemKey == itemKey && x.IsActive);
            if (entity == null) throw new ArgumentException("ไม่พบขั้นต่ำที่ตั้งไว้");

            entity.IsActive = false;
            entity.UpdateBy = CurrentUsername ?? "system";
            entity.UpdateDate = DateTime.UtcNow;
            _jewelryContext.TbmMaterialStockMin.Update(entity);

            await _jewelryContext.SaveChangesAsync();
            _insightCache.Invalidate(CachePrefix(kind));
            return ToMinResponse(entity);
        }

        private IMaterialInsightProvider GetProvider(string? kind)
        {
            var normalized = (kind ?? string.Empty).Trim().ToUpperInvariant();
            var provider = _providers.FirstOrDefault(p => p.Kind == normalized);
            if (provider == null) throw new ArgumentException($"ไม่รองรับประเภทวัตถุดิบ: {kind}");
            return provider;
        }

        private async Task<PolicyValues> ResolvePolicyAsync(string kind, PolicyOverrides? requestOverrides)
        {
            var row = await _jewelryContext.TbmMaterialStockPolicy
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MaterialKind == kind);

            return MaterialInsightThresholds.Resolve(row == null ? null : MaterialInsightThresholds.FromEntity(row), requestOverrides);
        }

        private static Min.Response ToMinResponse(TbmMaterialStockMin e) => new Min.Response
        {
            Kind = e.MaterialKind,
            ItemKey = e.ItemKey,
            MinQty = e.MinQty,
            LeadTimeDays = e.LeadTimeDays,
            Note = e.Note,
            IsActive = e.IsActive
        };
    }
}
