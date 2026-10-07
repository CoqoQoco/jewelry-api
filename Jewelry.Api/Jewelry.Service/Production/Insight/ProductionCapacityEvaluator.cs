using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับข้อมูลที่โหลด/คำนวณมาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // "ผลิตเสร็จ" (output) = เข้าสถานะบัตรต้นทุน (95) ครั้งแรก (MIN header.create_date ที่ Status=95 ต่อแผน) —
    // ต่างจาก "เสร็จสมบูรณ์" (completed) = done date (เอกสารเดียวกับ Delivery: transfer แรกเข้า 100 หรือ fallback
    // completed_date) — ช่องว่างระหว่างสองจุดนี้คือ "บัตรต้นทุน → สำเร็จ" (costCardToDone) ติดตามแยกต่างหาก
    public static class ProductionCapacityEvaluator
    {
        private const int StatusPrice = 95;
        private const int StatusMelted = 500;
        private const int StatusCompleted = 100;
        private const int StatusWaitCvd = 84;
        private const int StatusCvd = 85;

        // MIN(create_date) ของ header ที่ Status=95 ต่อแผน — reuse trendData.Headers ที่โหลดมาแล้ว ไม่ query ซ้ำ
        public static Dictionary<int, DateTime> ComputeFirst95Dates(IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers)
        {
            return headers
                .Where(h => h.Status == StatusPrice)
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.Min(h => h.CreateDate));
        }

        // MIN(create_date) ของ header ที่ Status=500 ต่อแผน — fallback completed_date ถ้าไม่มี header 500 เลย
        // (plan.Status=500 ปัจจุบันแต่ไม่เคยมี header 500 บันทึกไว้ — ข้อมูลเก่า/legacy)
        public static Dictionary<int, DateTime> ComputeMeltedDates(
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers)
        {
            var result = headers
                .Where(h => h.Status == StatusMelted)
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.Min(h => h.CreateDate));

            foreach (var plan in plans)
            {
                if (plan.Status == StatusMelted && !result.ContainsKey(plan.Id) && plan.CompletedDate.HasValue)
                {
                    result[plan.Id] = plan.CompletedDate.Value;
                }
            }

            return result;
        }

        public class MonthlySeriesPoint
        {
            public DateTime BucketEnd { get; set; }
            public int Inflow { get; set; }
            public int InflowPieces { get; set; }
            public int Output { get; set; }
            public int Completed { get; set; }
            public int Melted { get; set; }

            // null ถ้า bucketEnd อยู่ในอนาคต (ยังไม่เกิดขึ้นจริง จะ snapshot WIP ไม่ได้)
            public int? ActiveWipEnd { get; set; }
        }

        // wipByTime: ผลลัพธ์จาก ProductionPlanTrendEvaluator.ComputeWipCountsAtTimes(bucketEnds) ที่ caller
        // คำนวณมาแล้ว (ไม่ build timeline ซ้ำในนี้) — ActiveWipEnd ในนี้คือ WIP รวมทั้งหมด ไม่ตัด stale ออก
        // (ตัด stale ย้อนหลัง ณ เวลาในอดีตทำได้ยาก/กำกวม — ต่างจาก kpi.activeWip ที่เป็น snapshot ปัจจุบันตัด stale แล้ว)
        public static List<MonthlySeriesPoint> ComputeSeries(
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans,
            IReadOnlyDictionary<int, DateTime> first95Dates,
            IReadOnlyDictionary<int, DateTime> doneDates,
            IReadOnlyDictionary<int, DateTime> meltedDates,
            IReadOnlyDictionary<DateTime, ProductionPlanTrendEvaluator.WipSnapshot> wipByTime,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds,
            DateTime now)
        {
            var result = new List<MonthlySeriesPoint>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var inflowPlans = plans.Where(p => p.CreateDate > prev && p.CreateDate <= bucketEnd).ToList();
                var output = first95Dates.Values.Count(d => d > prev && d <= bucketEnd);
                var completed = doneDates.Values.Count(d => d > prev && d <= bucketEnd);
                var melted = meltedDates.Values.Count(d => d > prev && d <= bucketEnd);
                int? activeWipEnd = bucketEnd <= now && wipByTime.TryGetValue(bucketEnd, out var snap) ? snap.Total : (int?)null;

                result.Add(new MonthlySeriesPoint
                {
                    BucketEnd = bucketEnd,
                    Inflow = inflowPlans.Count,
                    InflowPieces = inflowPlans.Sum(p => p.ProductQty),
                    Output = output,
                    Completed = completed,
                    Melted = melted,
                    ActiveWipEnd = activeWipEnd
                });

                prev = bucketEnd;
            }

            return result;
        }

        // exit (visit จบ) ต่อเดือนของ 1 แผนก — bucket เดียวกับ series ระดับบน — deptVisits กรองมาแล้วเฉพาะแผนกนี้
        public static List<(DateTime BucketEnd, int Exits)> ComputeExitsSeries(
            IReadOnlyList<ProductionStageLeadTimeEvaluator.VisitRecord> deptVisits,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new List<(DateTime, int)>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var count = deptVisits.Count(v => v.End.HasValue && v.End.Value > prev && v.End.Value <= bucketEnd);
                result.Add((bucketEnd, count));
                prev = bucketEnd;
            }

            return result;
        }

        // ---- ด้านชิ้น + ตัวกรอง + breakdown (ProductQty ตามตัวเลขบนใบงานตรงๆ ไม่แปลงหน่วย) ----

        private static string NormKey(string? v) => (v ?? string.Empty).Trim().ToUpperInvariant();

        // null = ไม่มีตัวกรองเลย (ใช้ทุกแผน) — เทียบแบบไม่สนตัวพิมพ์/ช่องว่าง, array ว่าง/null ของมิติไหน = ไม่กรองมิตินั้น
        public static HashSet<int>? ResolveAllowedPlanIds(
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans, string[]? productTypes, string[]? golds, string[]? goldSizes)
        {
            HashSet<string>? Set(string[]? values) => values != null && values.Length > 0
                ? new HashSet<string>(values.Select(NormKey))
                : null;

            var pt = Set(productTypes);
            var g = Set(golds);
            var gs = Set(goldSizes);
            if (pt == null && g == null && gs == null) return null;

            return new HashSet<int>(plans
                .Where(p => (pt == null || pt.Contains(NormKey(p.ProductType)))
                    && (g == null || g.Contains(NormKey(p.Type)))
                    && (gs == null || gs.Contains(NormKey(p.TypeSize))))
                .Select(p => p.Id));
        }

        public class PiecesSeriesPoint
        {
            public DateTime BucketEnd { get; set; }
            public int OutputPieces { get; set; }
            public int CompletedPieces { get; set; }
            public int? ActiveWipEndPieces { get; set; }
        }

        private static int BucketIndexOf(DateTime d, DateTime start, IReadOnlyList<DateTime> bucketEnds)
        {
            var prev = start;
            for (var i = 0; i < bucketEnds.Count; i++)
            {
                if (d > prev && d <= bucketEnds[i]) return i;
                prev = bucketEnds[i];
            }
            return -1;
        }

        public static List<PiecesSeriesPoint> ComputePiecesSeries(
            IReadOnlyDictionary<int, int> qtyById,
            IReadOnlyDictionary<int, DateTime> first95Dates,
            IReadOnlyDictionary<int, DateTime> doneDates,
            IReadOnlyDictionary<DateTime, ProductionPlanTrendEvaluator.WipSnapshot> wipByTime,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds,
            DateTime now)
        {
            var result = new List<PiecesSeriesPoint>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var lo = prev;
                var hi = bucketEnd;
                var output = first95Dates.Where(kv => kv.Value > lo && kv.Value <= hi).Sum(kv => qtyById.TryGetValue(kv.Key, out var q) ? q : 0);
                var completed = doneDates.Where(kv => kv.Value > lo && kv.Value <= hi).Sum(kv => qtyById.TryGetValue(kv.Key, out var q) ? q : 0);
                int? wipPieces = bucketEnd <= now && wipByTime.TryGetValue(bucketEnd, out var snap) ? snap.TotalPieces : (int?)null;

                result.Add(new PiecesSeriesPoint { BucketEnd = bucketEnd, OutputPieces = output, CompletedPieces = completed, ActiveWipEndPieces = wipPieces });
                prev = bucketEnd;
            }

            return result;
        }

        public static string GroupKeyOf(ProductionPlanTrendEvaluator.PlanTrendRow p, string by)
        {
            var raw = by == "gold" ? p.Type : by == "goldSize" ? p.TypeSize : p.ProductType;
            return (raw ?? string.Empty).Trim();
        }

        public class BreakdownGroup
        {
            public string Key { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public int InflowPieces { get; set; }
            public int OutputPieces { get; set; }
            public int CompletedPieces { get; set; }
            public int ActiveWipPieces { get; set; }
            public int InflowPlans { get; set; }
            public int OutputPlans { get; set; }
            public int ActiveWipPlans { get; set; }
            // จำนวนแผนในแต่ละชุดที่ ProductQty = 0 (ใช้ตรวจ invariant pieces >= plans - zeroQtyPlans)
            public int InflowZeroQtyPlans { get; set; }
            public int OutputZeroQtyPlans { get; set; }
            public int ActiveWipZeroQtyPlans { get; set; }
            public int[] InflowPiecesByBucket { get; set; } = Array.Empty<int>();
            public int[] OutputPiecesByBucket { get; set; } = Array.Empty<int>();
        }

        // plans = แผนที่ผ่านตัวกรองแล้ว (กลุ่มจึงเคารพตัวกรอง) — first95/done ใช้เฉพาะแผนที่อยู่ใน plans — activeWip =
        // แผนเปิดไม่ stale ที่อยู่ใน dept (นิยามเดียวกับ kpi.activeWip) พร้อม qty ที่ส่งมาแล้ว
        public static List<BreakdownGroup> ComputeBreakdown(
            string by,
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> attributePlans,
            IReadOnlyDictionary<int, DateTime> first95Dates,
            IReadOnlyDictionary<int, DateTime> doneDates,
            IReadOnlyList<(int PlanId, int Qty)> activeWipPlans,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds)
        {
            // attributePlans = plans + แผนที่ถูกอ้างจาก output/completed แต่ไม่อยู่ใน plans (เช่นไม่อยู่ในชุด trend) — ใช้หา qty/หน่วย/กลุ่ม
            var planDict = attributePlans.ToDictionary(p => p.Id);
            var groups = new Dictionary<string, BreakdownGroup>();
            var unitsByGroup = new Dictionary<string, Dictionary<string, int>>();
            var seenPlans = new HashSet<(string, int)>();

            BreakdownGroup Get(int planId, out ProductionPlanTrendEvaluator.PlanTrendRow plan)
            {
                plan = planDict[planId];
                var key = GroupKeyOf(plan, by);
                if (!groups.TryGetValue(key, out var g))
                {
                    g = new BreakdownGroup
                    {
                        Key = key,
                        InflowPiecesByBucket = new int[bucketEnds.Count],
                        OutputPiecesByBucket = new int[bucketEnds.Count]
                    };
                    groups[key] = g;
                    unitsByGroup[key] = new Dictionary<string, int>();
                }
                // นับหน่วยต่อแผน (distinct) ไม่ใช่ต่อเหตุการณ์ — แผนเดียวเข้า inflow/output/active หลายที่ไม่ถ่วงน้ำหนักซ้ำ
                if (seenPlans.Add((key, planId)))
                {
                    var u = (plan.ProductQtyUnit ?? string.Empty).Trim();
                    unitsByGroup[key].TryGetValue(u, out var cnt);
                    unitsByGroup[key][u] = cnt + 1;
                }
                return g;
            }

            foreach (var p in plans)
            {
                var idx = BucketIndexOf(p.CreateDate, start, bucketEnds);
                if (idx < 0) continue;
                var g = Get(p.Id, out _);
                g.InflowPieces += p.ProductQty;
                g.InflowPlans++;
                if (p.ProductQty == 0) g.InflowZeroQtyPlans++;
                g.InflowPiecesByBucket[idx] += p.ProductQty;
            }

            var rangeEnd = bucketEnds.Count > 0 ? bucketEnds[bucketEnds.Count - 1] : start;

            foreach (var kv in first95Dates)
            {
                if (!planDict.ContainsKey(kv.Key)) continue;
                var idx = BucketIndexOf(kv.Value, start, bucketEnds);
                if (idx < 0) continue;
                var g = Get(kv.Key, out var plan);
                g.OutputPieces += plan.ProductQty;
                g.OutputPlans++;
                if (plan.ProductQty == 0) g.OutputZeroQtyPlans++;
                g.OutputPiecesByBucket[idx] += plan.ProductQty;
            }

            foreach (var kv in doneDates)
            {
                if (!planDict.ContainsKey(kv.Key)) continue;
                if (kv.Value <= start || kv.Value > rangeEnd) continue;
                var g = Get(kv.Key, out var plan);
                g.CompletedPieces += plan.ProductQty;
            }

            foreach (var (planId, qty) in activeWipPlans)
            {
                if (!planDict.ContainsKey(planId)) continue;
                var g = Get(planId, out _);
                g.ActiveWipPieces += qty;
                g.ActiveWipPlans++;
                if (qty == 0) g.ActiveWipZeroQtyPlans++;
            }

            foreach (var g in groups.Values)
            {
                var counts = unitsByGroup[g.Key];
                var total = counts.Values.Sum();
                var top = counts.OrderByDescending(c => c.Value).First();
                // หน่วยที่พบมากสุดต้องเกินครึ่งของแผนในกลุ่ม ไม่งั้น 'mixed'
                g.Unit = total > 0 && top.Value * 2 > total ? top.Key : "mixed";
            }

            return groups.Values.OrderByDescending(g => g.ActiveWipPieces).ToList();
        }

        // invariant: ทุกแผนที่นับในด้านแผนต้องมีส่วนในด้านชิ้น — pieces >= plans - (จำนวนแผนที่ qty=0) ต่อกลุ่ม
        // คืนรายการที่ละเมิด (ว่าง = ผ่าน)
        public static List<string> CheckBreakdownInvariants(IEnumerable<BreakdownGroup> groups)
        {
            var violations = new List<string>();
            foreach (var g in groups)
            {
                if (g.InflowPieces < g.InflowPlans - g.InflowZeroQtyPlans) violations.Add($"{g.Key}:inflow");
                if (g.OutputPieces < g.OutputPlans - g.OutputZeroQtyPlans) violations.Add($"{g.Key}:output");
                if (g.ActiveWipPieces < g.ActiveWipPlans - g.ActiveWipZeroQtyPlans) violations.Add($"{g.Key}:activeWip");
            }
            return violations;
        }

        public class WorkersResult
        {
            // null ถ้าไม่มี bucket ไหนมีข้อมูลเลย (ไม่ควรเกิดถ้า bucketEnds ไม่ว่าง — median ของ 0 ได้ปกติ)
            public double? WorkersMedian { get; set; }
            public List<(DateTime BucketEnd, int Workers)> Series { get; set; } = new List<(DateTime, int)>();
        }

        // activityRows: (deptKey, header.create_date, header.worker_code) ของทุก header ที่ active ในช่วง — ดึงมาจาก
        // tbt_production_plan_status_header.worker_code (ไม่ใช้ status_detail.worker — เลือกตัวที่เบากว่า/พอสำหรับ
        // ภาพรวมรายเดือน; เอกสารไว้ใน service) — นับ distinct worker ต่อแผนกต่อ bucket แล้วหา median ตลอดช่วง
        public static Dictionary<string, WorkersResult> ComputeWorkersByDept(
            IReadOnlyList<(string DeptKey, DateTime CreateDate, string? WorkerCode)> activityRows,
            IReadOnlyList<(string Key, int[] StatusIds)> departments,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new Dictionary<string, WorkersResult>();

            foreach (var dept in departments)
            {
                var deptRows = activityRows.Where(r => r.DeptKey == dept.Key).ToList();
                var series = new List<(DateTime, int)>();
                var prev = start;

                foreach (var bucketEnd in bucketEnds)
                {
                    var bucketWorkers = deptRows
                        .Where(r => r.CreateDate > prev && r.CreateDate <= bucketEnd && !string.IsNullOrWhiteSpace(r.WorkerCode))
                        .Select(r => r.WorkerCode!.Trim())
                        .Distinct()
                        .Count();

                    series.Add((bucketEnd, bucketWorkers));
                    prev = bucketEnd;
                }

                var values = series.Select(s => (double)s.Item2).ToList();
                result[dept.Key] = new WorkersResult
                {
                    WorkersMedian = values.Count > 0 ? ProductionStageLeadTimeEvaluator.Median(values) : (double?)null,
                    Series = series
                };
            }

            return result;
        }

        public class CostCardToDoneStats
        {
            public double? MedianDays { get; set; }
            public double? P90Days { get; set; }

            // ทั้งหมด (รวม stale) — ตัวเลขดิบ ไม่ใช้ตัดสินใจเร่งรัดตรงๆ เพราะปนแผนค้างเก่าตั้งแต่ปี 2024
            public int PendingNow { get; set; }

            // ย่อยจาก PendingNow: ไม่ stale (last move < 180 วัน) = ของที่ยังมีความเคลื่อนไหวจริง ควรเร่งรัด
            public int PendingActive { get; set; }

            // ย่อยจาก PendingNow: stale — ค้างนานจนน่าจะต้องจัดการแยกต่างหาก (ไม่ใช่ปัญหา "ช้า" ธรรมดา)
            public int PendingStale { get; set; }

            public int PendingOver30d { get; set; }
            public List<(DateTime BucketEnd, int Count, double? MedianDays, double? P90Days)> Series { get; set; }
                = new List<(DateTime, int, double?, double?)>();
        }

        // คู่ (first95Date, doneDate) ของแผนที่มีครบทั้งสอง — gap = doneDate - first95Date (วัน) — bucket ตาม doneDate
        // (ช่วงที่ "รู้ผลแล้ว" ว่าใช้เวลาเท่าไหร่) — pending = เข้า 95 แล้วแต่ยังไม่มี doneDate และสถานะปัจจุบันไม่ใช่
        // 100/500/84/85 (เหมือนนิยาม stuck-after-costcard เดิม) — stalePlanIds: นิยามเดียวกับ WIP_STALE/kpi.activeWip
        // (last move >= 180 วัน) ใช้แยก pendingActive/pendingStale
        public static CostCardToDoneStats ComputeCostCardToDone(
            IReadOnlyDictionary<int, DateTime> first95Dates,
            IReadOnlyDictionary<int, DateTime> doneDates,
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds,
            DateTime now,
            int pendingOver30dThresholdDays,
            IReadOnlyCollection<int> stalePlanIds)
        {
            var gaps = new List<(DateTime DoneDate, double Days)>();
            foreach (var kv in first95Dates)
            {
                if (doneDates.TryGetValue(kv.Key, out var done) && done >= kv.Value)
                {
                    gaps.Add((done, (done - kv.Value).TotalDays));
                }
            }

            var allDays = gaps.Select(g => g.Days).ToList();
            var medianDays = allDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Median(allDays) : (double?)null;
            var p90Days = allDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Percentile(allDays, 90) : (double?)null;

            var excluded = new HashSet<int> { StatusCompleted, StatusMelted, StatusWaitCvd, StatusCvd };
            var pendingPlans = plans
                .Where(p => first95Dates.ContainsKey(p.Id) && !doneDates.ContainsKey(p.Id) && !excluded.Contains(p.Status))
                .ToList();
            var pendingNow = pendingPlans.Count;
            var pendingStale = pendingPlans.Count(p => stalePlanIds.Contains(p.Id));
            var pendingActive = pendingNow - pendingStale;
            var pendingOver30d = pendingPlans.Count(p => (now - first95Dates[p.Id]).TotalDays > pendingOver30dThresholdDays);

            var series = new List<(DateTime, int, double?, double?)>();
            var prev = start;
            foreach (var bucketEnd in bucketEnds)
            {
                var bucketDays = gaps.Where(g => g.DoneDate > prev && g.DoneDate <= bucketEnd).Select(g => g.Days).ToList();
                series.Add((
                    bucketEnd,
                    bucketDays.Count,
                    bucketDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Median(bucketDays) : (double?)null,
                    bucketDays.Count > 0 ? ProductionStageLeadTimeEvaluator.Percentile(bucketDays, 90) : (double?)null));
                prev = bucketEnd;
            }

            return new CostCardToDoneStats
            {
                MedianDays = medianDays,
                P90Days = p90Days,
                PendingNow = pendingNow,
                PendingActive = pendingActive,
                PendingStale = pendingStale,
                PendingOver30d = pendingOver30d,
                Series = series
            };
        }
    }
}
