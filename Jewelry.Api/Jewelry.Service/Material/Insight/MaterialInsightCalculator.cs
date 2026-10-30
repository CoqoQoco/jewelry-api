using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Material.Insight;
using jewelry.Model.Material.Insight.Overview;
using Jewelry.Service.Stock.Insight;

namespace Jewelry.Service.Material.Insight
{
    public record MaterialMinOverride(decimal MinQty, int? LeadTimeDays);

    // Pure — ไม่แตะ DB (unit-testable)
    public static class MaterialInsightCalculator
    {
        public const string StatusNegative = "negative";
        public const string StatusZero = "zero";
        public const string StatusBelow = "below";
        public const string StatusNear = "near";
        public const string StatusOk = "ok";
        public const string StatusDead = "dead";

        public const string MinSourceAuto = "auto";
        public const string MinSourceOverride = "override";
        public const string MinSourceFloor = "floor";

        private const decimal DaysPerMonth = 30m;

        // เดือน (เวลาไทย) ใช้ตัวช่วยเดียวกับ StockInsight เพื่อให้กรุ๊ปเดือนตรงกันทุกแท็บ
        public static List<DateTime> MonthStarts(DateTime utcNow, int months) => StockInsightCalculator.MonthStarts(utcNow, months);

        public static DateTime ThaiMonthStart(DateTime utc) => StockInsightCalculator.ThaiMonthStart(utc);

        public static int FullMonthsSince(DateTime? startMonth, DateTime currentMonth) => StockInsightCalculator.FullMonthsSince(startMonth, currentMonth);

        // วันเบิกแรกสุดที่ยังมีผล (Consumed) — null = ยังไม่เคยเบิก
        public static DateTime? FirstConsumedUtc(IEnumerable<MaterialMovement> movements)
        {
            DateTime? first = null;
            foreach (var m in movements)
            {
                if (m.Kind != MaterialMovementKind.Consumed) continue;
                if (first == null || m.DateUtc < first.Value) first = m.DateUtc;
            }
            return first;
        }

        // หน้าต่างเฉลี่ยอัตราใช้ = windowMonths ย้อนจากวันนี้ แต่ไม่เริ่มก่อน "ต้นเดือนที่เบิกครั้งแรก"
        // (กันระบบใหม่ที่มีประวัติสั้นกว่าหน้าต่าง ทำให้อัตราใช้ถูกหารด้วยวันที่ยังไม่มีข้อมูลจนต่ำเกินจริง)
        public static DateTime EffectiveWindowStartUtc(DateTime nowUtc, int windowMonths, DateTime? firstConsumedUtc)
        {
            var start = nowUtc.AddMonths(-windowMonths);
            if (firstConsumedUtc.HasValue)
            {
                var usageStart = StockInsightCalculator.WindowStartUtc(ThaiMonthStart(firstConsumedUtc.Value));
                if (usageStart > start) start = usageStart;
            }
            return start;
        }

        // จำนวนวันของหน้าต่าง (ขั้นต่ำ 1 วัน กันหารศูนย์)
        public static decimal WindowDays(DateTime windowStartUtc, DateTime nowUtc)
            => Math.Max(1m, (decimal)(nowUtc - windowStartUtc).TotalDays);

        public static decimal AvgDailyUsage(decimal consumedInWindow, decimal windowDays)
            => windowDays <= 0 || consumedInWindow <= 0 ? 0m : consumedInWindow / windowDays;

        // ขั้นต่ำอัตโนมัติ = ceil(ใช้เฉลี่ย/วัน x (lead time + safety))
        public static decimal AutoMin(decimal avgDailyUsage, int leadTimeDays, int safetyDays)
        {
            if (avgDailyUsage <= 0) return 0m;
            return Math.Ceiling(avgDailyUsage * (Math.Max(0, leadTimeDays) + Math.Max(0, safetyDays)));
        }

        // override (แม้เป็น 0) > max(auto, floor); source = floor เมื่อ floor > auto
        public static (decimal Value, string Source) EffectiveMin(decimal autoMin, decimal floorQty, decimal? overrideMin)
        {
            if (overrideMin.HasValue) return (overrideMin.Value, MinSourceOverride);
            if (floorQty > autoMin) return (floorQty, MinSourceFloor);
            return (autoMin, MinSourceAuto);
        }

        public static decimal? CoverDays(decimal available, decimal avgDailyUsage)
            => avgDailyUsage > 0 && available > 0 ? available / avgDailyUsage : null;

        public static bool IsDead(decimal onHand, int? daysSinceMove, int deadStockDays)
            => onHand > 0 && daysSinceMove.HasValue && daysSinceMove.Value > deadStockDays;

        // negative: ยอดติดลบ (ตรวจก่อนทุกสถานะ — ledger ไม่มียอดตั้งต้น ไม่ใช่ของหมด)
        // zero: หมด (available <= 0) และ "ควรมี" ของ (มีขั้นต่ำ หรือมีการใช้ในหน้าต่าง) — หมดแต่ไม่เคยใช้/ไม่มีขั้นต่ำ = ok
        // below: available < min | near: available < min x nearPercent/100 | dead: ไม่ได้ใช้นานเกินเกณฑ์ (เมื่อไม่ติดสถานะก่อนหน้า)
        public static string Classify(decimal available, decimal effectiveMin, decimal avgDailyUsage, decimal nearPercent, bool isDead)
        {
            if (available < 0) return StatusNegative;
            if (available <= 0) return effectiveMin > 0 || avgDailyUsage > 0 ? StatusZero : StatusOk;
            if (available < effectiveMin) return StatusBelow;
            if (effectiveMin > 0 && available < effectiveMin * nearPercent / 100m) return StatusNear;
            return isDead ? StatusDead : StatusOk;
        }

        public static string Label(MaterialItemInput i)
        {
            var parts = new[] { string.IsNullOrWhiteSpace(i.Name) ? i.Code : i.Name, string.IsNullOrWhiteSpace(i.SizeName) ? i.SizeCode : i.SizeName }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join(" ", parts);
        }

        public static List<Item> BuildItems(
            string kind,
            IReadOnlyList<MaterialItemInput> inputs,
            IReadOnlyList<MaterialMovement> movements,
            IReadOnlyDictionary<string, MaterialMinOverride> overrides,
            PolicyValues policy,
            DateTime nowUtc)
        {
            var windowStart = EffectiveWindowStartUtc(nowUtc, policy.WindowMonths, FirstConsumedUtc(movements));
            var windowDays = WindowDays(windowStart, nowUtc);
            var byItem = movements.ToLookup(m => m.ItemKey);

            var result = new List<Item>(inputs.Count);
            foreach (var input in inputs)
            {
                var mv = byItem[input.ItemKey].ToList();
                var consumed = mv.Where(m => m.Kind == MaterialMovementKind.Consumed && m.DateUtc >= windowStart && m.DateUtc <= nowUtc).Sum(m => m.Qty);
                var returned = mv.Where(m => m.Kind == MaterialMovementKind.Returned && m.DateUtc >= windowStart && m.DateUtc <= nowUtc).Sum(m => m.Qty);
                var net = Math.Max(0m, consumed - returned);

                DateTime? lastConsumed = null;
                foreach (var m in mv)
                {
                    if (m.Kind != MaterialMovementKind.Consumed) continue;
                    if (lastConsumed == null || m.DateUtc > lastConsumed.Value) lastConsumed = m.DateUtc;
                }

                var refDate = lastConsumed ?? input.FirstSeenUtc;
                int? daysSince = refDate.HasValue ? Math.Max(0, (int)Math.Floor((nowUtc - refDate.Value).TotalDays)) : null;

                overrides.TryGetValue(input.ItemKey, out var ov);
                var leadTime = ov?.LeadTimeDays ?? policy.LeadTimeDays;

                var avgDaily = AvgDailyUsage(net, windowDays);
                var autoMin = AutoMin(avgDaily, leadTime, policy.SafetyDays);
                var (effMin, minSource) = EffectiveMin(autoMin, policy.FloorQty, ov?.MinQty);

                var available = input.OnHand;
                var negative = available < 0;
                var dead = !negative && IsDead(input.OnHand, daysSince, policy.DeadStockDays);
                var status = Classify(available, effMin, avgDaily, policy.NearPercent, dead);
                var cover = negative ? null : CoverDays(available, avgDaily);

                result.Add(new Item
                {
                    ItemKey = input.ItemKey,
                    Code = input.Code,
                    Name = input.Name,
                    SizeCode = input.SizeCode,
                    SizeName = input.SizeName,
                    Label = Label(input),
                    Unit = input.Unit,
                    GroupCode = input.GroupCode,
                    GroupName = input.GroupName,
                    UnitValue = input.UnitValue,
                    Value = ValueOf(input.OnHand, input.UnitValue),
                    OnProcessValue = ValueOf(input.OnProcess, input.UnitValue),
                    GoldCode = kind == "GOLD" ? input.Code : null,
                    GoldName = kind == "GOLD" ? input.Name : null,
                    OnHand = input.OnHand,
                    OnProcess = input.OnProcess,
                    Available = available,
                    ConsumedInWindow = net,
                    AvgDailyUsage = Math.Round(avgDaily, 4, MidpointRounding.AwayFromZero),
                    AvgMonthlyUsage = Math.Round(avgDaily * DaysPerMonth, 2, MidpointRounding.AwayFromZero),
                    CoverDays = cover.HasValue ? Math.Round(cover.Value, 1, MidpointRounding.AwayFromZero) : null,
                    LeadTimeDays = leadTime,
                    OverrideLeadTimeDays = ov?.LeadTimeDays,
                    AutoMin = autoMin,
                    OverrideMin = ov?.MinQty,
                    EffectiveMin = effMin,
                    MinSource = minSource,
                    Status = status,
                    IsDead = dead,
                    StockoutRisk = !negative && cover.HasValue && cover.Value < leadTime,
                    LastMovementDate = lastConsumed,
                    DaysSinceMove = daysSince,
                    NeverConsumed = lastConsumed == null,
                    OnProcessAgeDays = input.OnProcessAgeDays
                });
            }

            return result;
        }

        public static decimal? ValueOf(decimal qty, decimal? unitValue)
            => unitValue.HasValue ? Math.Round(qty * unitValue.Value, 2, MidpointRounding.AwayFromZero) : null;

        public static List<string> DistinctUnits(IEnumerable<string> units)
            => units.Distinct(StringComparer.Ordinal).OrderBy(u => u, StringComparer.Ordinal).ToList();

        // series แยกรายหน่วย: movement/ยอดปัจจุบันเฉพาะ item ของหน่วยนั้น (qty คนละหน่วยห้ามรวมกัน)
        public static List<UnitSeries> BuildSeriesByUnit(IReadOnlyList<DateTime> months, IReadOnlyList<MaterialItemInput> inputs, IEnumerable<MaterialMovement> movements)
        {
            var unitByKey = inputs.ToDictionary(i => i.ItemKey, i => i.Unit);
            var list = movements.ToList();
            return DistinctUnits(inputs.Select(i => i.Unit))
                .Select(u => new UnitSeries
                {
                    Unit = u,
                    Points = BuildSeries(months,
                        list.Where(m => unitByKey.TryGetValue(m.ItemKey, out var mu) && mu == u),
                        inputs.Where(i => i.Unit == u).Sum(i => i.OnHand))
                })
                .ToList();
        }

        // series มูลค่า (บาท): movement x ราคาต่อหน่วยของ item (ราคาปัจจุบัน) — null เมื่อไม่มี item ที่มีราคา
        public static List<ValuePoint>? BuildValueSeries(IReadOnlyList<DateTime> months, IReadOnlyList<MaterialItemInput> inputs, IEnumerable<MaterialMovement> movements)
        {
            if (!inputs.Any(i => i.UnitValue.HasValue)) return null;

            var priceByKey = inputs.Where(i => i.UnitValue.HasValue).ToDictionary(i => i.ItemKey, i => i.UnitValue!.Value);
            var points = months.Select(m => new ValuePoint { Month = m.ToString("yyyy-MM-dd") }).ToList();
            var netByMonth = new decimal[months.Count];
            var indexByMonth = months.Select((m, i) => (m, i)).ToDictionary(x => x.m, x => x.i);

            foreach (var mv in movements)
            {
                if (!priceByKey.TryGetValue(mv.ItemKey, out var price)) continue;
                if (!indexByMonth.TryGetValue(ThaiMonthStart(mv.DateUtc), out var idx)) continue;

                var v = mv.Qty * price;
                netByMonth[idx] += mv.Sign * v;
                switch (mv.Kind)
                {
                    case MaterialMovementKind.Inflow: points[idx].InflowValue += v; break;
                    case MaterialMovementKind.Consumed: points[idx].ConsumedValue += v; break;
                    case MaterialMovementKind.Returned: points[idx].ReturnedValue += v; break;
                }
            }

            var after = 0m;
            var current = inputs.Where(i => i.UnitValue.HasValue).Sum(i => i.OnHand * i.UnitValue!.Value);
            for (var i = months.Count - 1; i >= 0; i--)
            {
                points[i].EndStockValue = Math.Round(current - after, 2, MidpointRounding.AwayFromZero);
                points[i].InflowValue = Math.Round(points[i].InflowValue, 2, MidpointRounding.AwayFromZero);
                points[i].ConsumedValue = Math.Round(points[i].ConsumedValue, 2, MidpointRounding.AwayFromZero);
                points[i].ReturnedValue = Math.Round(points[i].ReturnedValue, 2, MidpointRounding.AwayFromZero);
                after += netByMonth[i];
            }

            return points;
        }

        // endStock(เดือน m) = ยอดคงเหลือปัจจุบัน - ผลรวมสุทธิ (sign x qty) ของ movement ที่เกิดหลังสิ้นเดือน m
        public static List<SeriesPoint> BuildSeries(IReadOnlyList<DateTime> months, IEnumerable<MaterialMovement> movements, decimal currentOnHand)
        {
            var points = months.Select(m => new SeriesPoint { Month = m.ToString("yyyy-MM-dd") }).ToList();
            var netByMonth = new decimal[months.Count];
            var indexByMonth = months.Select((m, i) => (m, i)).ToDictionary(x => x.m, x => x.i);

            foreach (var mv in movements)
            {
                if (!indexByMonth.TryGetValue(ThaiMonthStart(mv.DateUtc), out var idx)) continue;

                netByMonth[idx] += mv.Sign * mv.Qty;
                switch (mv.Kind)
                {
                    case MaterialMovementKind.Inflow: points[idx].Inflow += mv.Qty; break;
                    case MaterialMovementKind.Consumed: points[idx].Consumed += mv.Qty; break;
                    case MaterialMovementKind.Returned: points[idx].Returned += mv.Qty; break;
                    default: points[idx].Other += mv.Sign * mv.Qty; break;
                }
            }

            var after = 0m;
            for (var i = months.Count - 1; i >= 0; i--)
            {
                points[i].EndStock = currentOnHand - after;
                after += netByMonth[i];
            }

            return points;
        }

        public static UnitTotal BuildUnitTotal(string unit, IReadOnlyList<Item> items, bool onProcessAgeSupported, PolicyValues policy)
        {
            var nonNegative = items.Where(i => i.Status != StatusNegative).ToList();
            var negatives = items.Where(i => i.Status == StatusNegative).ToList();
            var stale = onProcessAgeSupported
                ? items.Where(i => i.OnProcess > 0 && i.OnProcessAgeDays.HasValue && i.OnProcessAgeDays.Value > policy.OnProcessStaleDays).ToList()
                : new List<Item>();
            var dead = items.Where(i => i.IsDead).ToList();
            var priced = items.Any(i => i.Value.HasValue);

            return new UnitTotal
            {
                Unit = unit,
                ItemCount = items.Count,
                TotalOnHand = nonNegative.Sum(i => i.OnHand),
                TotalOnProcess = items.Sum(i => i.OnProcess),
                TotalAvailable = nonNegative.Sum(i => i.Available),
                DeadCount = dead.Count,
                DeadOnHand = dead.Sum(i => i.OnHand),
                OnProcessStaleCount = stale.Count,
                OnProcessStaleWeight = stale.Sum(i => i.OnProcess),
                NegativeCount = negatives.Count,
                NegativeWeight = negatives.Sum(i => i.OnHand),
                Value = priced ? nonNegative.Sum(i => i.Value ?? 0m) : null,
                OnProcessValue = priced ? items.Sum(i => i.OnProcessValue ?? 0m) : null
            };
        }

        public static Kpi BuildKpi(IReadOnlyList<Item> items, bool onProcessAgeSupported, PolicyValues policy, bool inboundRecorded = false)
        {
            var byUnit = items
                .GroupBy(i => i.Unit, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => BuildUnitTotal(g.Key, g.ToList(), onProcessAgeSupported, policy))
                .ToList();
            var mixed = byUnit.Count > 1;

            var nonNegative = items.Where(i => i.Status != StatusNegative).ToList();
            var negatives = items.Where(i => i.Status == StatusNegative).ToList();
            var stale = onProcessAgeSupported
                ? items.Where(i => i.OnProcess > 0 && i.OnProcessAgeDays.HasValue && i.OnProcessAgeDays.Value > policy.OnProcessStaleDays).ToList()
                : new List<Item>();
            var dead = items.Where(i => i.IsDead).ToList();
            var priced = items.Any(i => i.Value.HasValue);

            // ผลรวม qty ข้ามหน่วยไม่มีความหมาย -> 0 เมื่อหลายหน่วย (ใช้ ByUnit)
            decimal Qty(Func<IEnumerable<Item>, decimal> f, IEnumerable<Item> src) => mixed ? 0m : f(src);

            return new Kpi
            {
                ItemCount = items.Count,
                TotalOnHand = Qty(l => l.Sum(i => i.OnHand), nonNegative),
                TotalOnProcess = Qty(l => l.Sum(i => i.OnProcess), items),
                TotalAvailable = Qty(l => l.Sum(i => i.Available), nonNegative),
                ZeroCount = items.Count(i => i.Status == StatusZero),
                BelowMinCount = items.Count(i => i.Status == StatusBelow),
                NearMinCount = items.Count(i => i.Status == StatusNear),
                DeadCount = dead.Count,
                DeadOnHand = Qty(l => l.Sum(i => i.OnHand), dead),
                OnProcessStaleCount = stale.Count,
                OnProcessStaleWeight = Qty(l => l.Sum(i => i.OnProcess), stale),
                OnProcessAgeSupported = onProcessAgeSupported,
                StockoutRiskCount = items.Count(i => i.StockoutRisk && i.Available > 0),
                OverrideCount = items.Count(i => i.MinSource == MinSourceOverride),
                WithUsageCount = items.Count(i => i.AvgDailyUsage > 0),
                NegativeCount = negatives.Count,
                NegativeWeight = Qty(l => l.Sum(i => i.OnHand), negatives),
                InboundRecorded = inboundRecorded,
                MixedUnits = mixed,
                ByUnit = byUnit,
                TotalValue = priced ? nonNegative.Sum(i => i.Value ?? 0m) : null,
                TotalOnProcessValue = priced ? items.Sum(i => i.OnProcessValue ?? 0m) : null,
                DeadValue = priced ? dead.Sum(i => i.Value ?? 0m) : null
            };
        }

        public static List<GroupRow> BuildGroups(IReadOnlyList<Item> items)
        {
            return items
                .GroupBy(i => i.GroupCode ?? i.Code)
                .Select(g =>
                {
                    var byUnit = g.GroupBy(i => i.Unit, StringComparer.Ordinal)
                        .OrderBy(u => u.Key, StringComparer.Ordinal)
                        .Select(u => new UnitTotal
                        {
                            Unit = u.Key,
                            ItemCount = u.Count(),
                            TotalOnHand = u.Sum(i => i.OnHand),
                            TotalOnProcess = u.Sum(i => i.OnProcess),
                            NegativeCount = u.Count(i => i.Status == StatusNegative),
                            Value = u.Any(i => i.Value.HasValue) ? u.Sum(i => i.Value ?? 0m) : null
                        })
                        .ToList();
                    var single = byUnit.Count == 1;
                    return new GroupRow
                    {
                        Code = g.Key,
                        Name = g.Select(i => i.GroupName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                            ?? g.Select(i => i.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                        ItemCount = g.Count(),
                        OnHand = single ? g.Sum(i => i.OnHand) : 0m,
                        OnProcess = single ? g.Sum(i => i.OnProcess) : 0m,
                        NegativeCount = g.Count(i => i.Status == StatusNegative),
                        Unit = single ? byUnit[0].Unit : null,
                        ByUnit = byUnit,
                        Value = g.Any(i => i.Value.HasValue) ? g.Sum(i => i.Value ?? 0m) : null
                    };
                })
                .OrderByDescending(g => g.Value ?? g.OnHand)
                .ThenByDescending(g => g.OnHand)
                .ThenBy(g => g.Code, StringComparer.Ordinal)
                .ToList();
        }
    }
}
