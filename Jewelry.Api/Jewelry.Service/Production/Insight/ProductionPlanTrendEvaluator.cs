using System;
using System.Collections.Generic;
using System.Linq;
using WipTrend = jewelry.Model.Production.Insight.WipTrend;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับ plan/header rows ที่ query มาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // สถานะของแผน ณ เวลา t = สถานะของ header ที่ active ล่าสุดที่ create_date <= t; ถ้ายังไม่มี header เลย
    // ถือว่าอยู่สถานะเริ่มต้น = 10 (ออกแบบ) ตั้งแต่ plan.create_date — จำลองเป็น "synthetic entry" ตัวแรกของ
    // timeline แต่ละแผน — **นับเป็น transition จริง** (ไม่มีอะไรเลย -> ออกแบบ) เพื่อให้แผนกออกแบบมี inflow ตอนแผน
    // ถือกำเนิด (เดิมไม่เคยนับ ทำให้แผนกออกแบบมี outflow แต่ไม่มี inflow เลย ไม่ reconcile กับ WIP จริง)
    //
    // ถ้าแผนออกจาก WIP (เสร็จ/หลอม) โดยไม่มี header บันทึกสถานะ 100/500 ไว้เลย แต่มี completed_date จะเติม
    // synthetic "exit" entry ที่ completed_date เพื่อให้นับ outflow จากแผนกสุดท้ายได้ถูกต้อง (แผนหลอมที่ไม่มีทั้ง
    // header 500 และ completed_date จะยังไม่ถูกนับ exit — เป็น known gap ตรวจสอบด้วย CheckDepartmentInvariant)
    //
    // แผนไม่ใช่ WIP ที่เวลา t ถ้า: ยังไม่ถือกำเนิด (create_date > t), หรือสถานะ ณ t อยู่ใน {100 เสร็จ, 500 หลอม},
    // หรือ completed_date <= t — เฉพาะแผน is_active เท่านั้น (แผน inactive ไม่นับเลย — known approximation)
    public static class ProductionPlanTrendEvaluator
    {
        private const int StatusDesign = 10;
        private const int StatusCompleted = 100;
        private const int StatusMelted = 500;

        // สถานะปลอมสำหรับแทน "ออกจาก WIP แล้วแต่ไม่มี header ยืนยัน" (ไม่ตรงกับ status code จริงใดๆ ทั้งสิ้น
        // เพื่อไม่ให้ปนกับ 100/500 จริงตอน debug) — DepartmentKeyOf คืน null เสมอสำหรับค่านี้ (ไม่อยู่แผนกไหน)
        private const int StatusExitedNoHeader = -1;

        public class PlanTrendRow
        {
            public int Id { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime? CompletedDate { get; set; }
        }

        public class WipSnapshot
        {
            public int Total { get; set; }
            public Dictionary<string, int> ByDept { get; set; } = new Dictionary<string, int>();
        }

        private class TimelineEntry
        {
            public DateTime Time;
            public int Status;
        }

        // ---- Bucket generation ----

        public class BucketDef
        {
            public DateTime End { get; set; }
            public string Label { get; set; } = null!;
        }

        // week: หน้าต่าง 7 วันต่อเนื่องนับถอยจาก end ไปหา start (อันแรกสุด/เก่าสุดอาจไม่เต็ม 7 วัน)
        // month: เดือนปฏิทินไทย นับถอยจาก end (อันแรกสุด/เก่าสุด และท้ายสุดอาจไม่เต็มเดือน)
        // ทั้งคู่ generate ถอยจาก end มาหา start แล้ว cap ที่ 60 — ถ้าช่วงยาวเกิน จะตัดเดือน/สัปดาห์เก่าสุดทิ้ง
        // (เก็บข้อมูลล่าสุดไว้เสมอ)
        public static List<BucketDef> GenerateBuckets(DateTime start, DateTime end, string bucket)
        {
            if (start >= end)
            {
                return new List<BucketDef> { new BucketDef { End = end, Label = FormatLabel(end, bucket) } };
            }

            var isMonth = string.Equals(bucket, "month", StringComparison.OrdinalIgnoreCase);

            var result = new List<BucketDef>();

            if (isMonth)
            {
                var startThai = start.AddHours(7);
                var endThai = end.AddHours(7);
                var cursorMonthStartThai = new DateTime(endThai.Year, endThai.Month, 1);
                var currentBucketEndThai = endThai;
                var startMonthThai = new DateTime(startThai.Year, startThai.Month, 1);

                while (true)
                {
                    var bucketEndUtc = DateTime.SpecifyKind(currentBucketEndThai.AddHours(-7), DateTimeKind.Utc);
                    result.Add(new BucketDef { End = bucketEndUtc, Label = cursorMonthStartThai.ToString("yyyy-MM") });

                    if (cursorMonthStartThai <= startMonthThai || result.Count >= 60) break;

                    currentBucketEndThai = cursorMonthStartThai;
                    cursorMonthStartThai = cursorMonthStartThai.AddMonths(-1);
                }
            }
            else
            {
                var cursor = end;
                while (cursor > start && result.Count < 60)
                {
                    result.Add(new BucketDef { End = cursor, Label = FormatLabel(cursor, "week") });
                    cursor = cursor.AddDays(-7);
                }
            }

            result.Reverse();
            return result;
        }

        private static string FormatLabel(DateTime bucketEndUtc, string bucket)
        {
            var thai = bucketEndUtc.AddHours(7);
            return string.Equals(bucket, "month", StringComparison.OrdinalIgnoreCase)
                ? thai.ToString("yyyy-MM")
                : thai.ToString("dd/MM");
        }

        // ---- Timeline (สถานะของแผนตามเวลา) ----

        private static Dictionary<int, List<TimelineEntry>> BuildTimelines(
            IReadOnlyList<PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers)
        {
            var headersByPlan = headers
                .GroupBy(h => h.ProductionPlanId)
                .ToDictionary(g => g.Key, g => g.OrderBy(h => h.CreateDate).ToList());

            var timelines = new Dictionary<int, List<TimelineEntry>>(plans.Count);
            foreach (var plan in plans)
            {
                var timeline = new List<TimelineEntry> { new TimelineEntry { Time = plan.CreateDate, Status = StatusDesign } };
                var hasTerminalHeader = false;

                if (headersByPlan.TryGetValue(plan.Id, out var planHeaders))
                {
                    foreach (var h in planHeaders)
                    {
                        timeline.Add(new TimelineEntry { Time = h.CreateDate, Status = h.Status });
                        if (h.Status == StatusCompleted || h.Status == StatusMelted) hasTerminalHeader = true;
                    }
                }

                if (!hasTerminalHeader && plan.CompletedDate.HasValue)
                {
                    timeline.Add(new TimelineEntry { Time = plan.CompletedDate.Value, Status = StatusExitedNoHeader });
                }

                timeline.Sort((a, b) => a.Time.CompareTo(b.Time));
                timelines[plan.Id] = timeline;
            }

            return timelines;
        }

        private static int? StatusAt(List<TimelineEntry> timeline, DateTime t)
        {
            int? status = null;
            foreach (var entry in timeline)
            {
                if (entry.Time > t) break;
                status = entry.Status;
            }

            return status;
        }

        private static bool IsWipAt(PlanTrendRow plan, int? statusAtT, DateTime t)
        {
            if (!statusAtT.HasValue) return false;
            if (statusAtT.Value == StatusCompleted || statusAtT.Value == StatusMelted) return false;
            if (plan.CompletedDate.HasValue && plan.CompletedDate.Value <= t) return false;
            return true;
        }

        // WIP snapshot (รวม + แยกแผนก) ณ เวลาที่ระบุ หลายเวลาในคราวเดียว (build timeline ครั้งเดียว)
        public static Dictionary<DateTime, WipSnapshot> ComputeWipCountsAtTimes(
            IReadOnlyList<PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers,
            Func<int, string?> departmentKeyOf,
            IReadOnlyList<DateTime> times)
        {
            var timelines = BuildTimelines(plans, headers);
            var result = new Dictionary<DateTime, WipSnapshot>();

            foreach (var t in times)
            {
                if (result.ContainsKey(t)) continue;

                var snapshot = new WipSnapshot();
                foreach (var plan in plans)
                {
                    var statusAtT = StatusAt(timelines[plan.Id], t);
                    if (!IsWipAt(plan, statusAtT, t)) continue;

                    snapshot.Total++;
                    var deptKey = departmentKeyOf(statusAtT!.Value);
                    if (deptKey != null)
                    {
                        snapshot.ByDept.TryGetValue(deptKey, out var count);
                        snapshot.ByDept[deptKey] = count + 1;
                    }
                }

                result[t] = snapshot;
            }

            return result;
        }

        // ---- Flow แบ่งตาม bucket (ใช้ transition-detection pattern เดียวกับ ProductionPlanFlowCalculator.ComputeFlow
        // แต่มี synthetic entry เริ่มต้นที่แน่นอนเสมอ (design ที่ create_date) แทนที่จะเป็น prevDept=null) ----

        public static (Dictionary<string, (int Inflow, int Outflow)> Totals, List<Dictionary<string, (int Inflow, int Outflow)>> ByBucket)
            ComputeBucketedFlow(
                IReadOnlyList<PlanTrendRow> plans,
                IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers,
                Func<int, string?> departmentKeyOf,
                DateTime start,
                IReadOnlyList<DateTime> bucketEnds)
        {
            var timelines = BuildTimelines(plans, headers);
            var totals = new Dictionary<string, (int Inflow, int Outflow)>();
            var byBucket = bucketEnds.Select(_ => new Dictionary<string, (int Inflow, int Outflow)>()).ToList();

            void AddInflow(Dictionary<string, (int Inflow, int Outflow)> dict, string key)
            {
                dict.TryGetValue(key, out var cur);
                dict[key] = (cur.Inflow + 1, cur.Outflow);
            }

            void AddOutflow(Dictionary<string, (int Inflow, int Outflow)> dict, string key)
            {
                dict.TryGetValue(key, out var cur);
                dict[key] = (cur.Inflow, cur.Outflow + 1);
            }

            var rangeEnd = bucketEnds.Count > 0 ? bucketEnds[bucketEnds.Count - 1] : start;

            foreach (var plan in plans)
            {
                var timeline = timelines[plan.Id];
                // เริ่ม prevDept = null (ไม่มีอะไรเลยก่อนแผนถือกำเนิด) แล้ว process ทุก entry รวมตัวแรก (synthetic
                // ออกแบบที่ create_date) เป็น transition เดียวกันหมด — ตัวแรกจึงนับ inflow เข้าออกแบบได้ถูกต้อง
                string? prevDept = null;

                foreach (var curr in timeline)
                {
                    var currDept = departmentKeyOf(curr.Status);

                    if (curr.Time > start && curr.Time <= rangeEnd)
                    {
                        var bucketIndex = FindBucketIndex(bucketEnds, curr.Time);
                        if (bucketIndex >= 0)
                        {
                            if (currDept != null && currDept != prevDept)
                            {
                                AddInflow(byBucket[bucketIndex], currDept);
                                AddInflow(totals, currDept);
                            }

                            if (prevDept != null && prevDept != currDept)
                            {
                                AddOutflow(byBucket[bucketIndex], prevDept);
                                AddOutflow(totals, prevDept);
                            }
                        }
                    }

                    prevDept = currDept;
                }
            }

            return (totals, byBucket);
        }

        // total.inflow/outflow ไม่ใช่ sum ของ department flow (นั่นคือการ "ย้ายแผนก" ภายใน WIP ไม่ใช่การเข้า/
        // ออกจาก WIP pool) — ที่นี่นิยามใหม่ตรงๆ: inflow = แผนที่ "ถือกำเนิด" ในช่วง, outflow = แผนที่ "ออกจาก WIP"
        // ในช่วง (มี header 100/500 หรือ fallback completed_date — ดู BuildTimelines) ดังนั้น
        // total.delta ควรจะ ≈ total.net (ต่างกันได้เฉพาะ edge case เช่นแผนหลอมที่ไม่มีทั้ง header และ completed_date)
        public static (int TotalInflow, int TotalOutflow, int[] InflowByBucket, int[] OutflowByBucket) ComputeTotalCreateExitFlow(
            IReadOnlyList<PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds)
        {
            var timelines = BuildTimelines(plans, headers);
            var inflowByBucket = new int[bucketEnds.Count];
            var outflowByBucket = new int[bucketEnds.Count];
            var totalInflow = 0;
            var totalOutflow = 0;
            var rangeEnd = bucketEnds.Count > 0 ? bucketEnds[bucketEnds.Count - 1] : start;

            foreach (var plan in plans)
            {
                if (plan.CreateDate > start && plan.CreateDate <= rangeEnd)
                {
                    var idx = FindBucketIndex(bucketEnds, plan.CreateDate);
                    if (idx >= 0)
                    {
                        inflowByBucket[idx]++;
                        totalInflow++;
                    }
                }

                var exitEntry = timelines[plan.Id]
                    .Where(e => e.Status == StatusCompleted || e.Status == StatusMelted || e.Status == StatusExitedNoHeader)
                    .OrderBy(e => e.Time)
                    .FirstOrDefault();

                if (exitEntry != null && exitEntry.Time > start && exitEntry.Time <= rangeEnd)
                {
                    var idx = FindBucketIndex(bucketEnds, exitEntry.Time);
                    if (idx >= 0)
                    {
                        outflowByBucket[idx]++;
                        totalOutflow++;
                    }
                }
            }

            return (totalInflow, totalOutflow, inflowByBucket, outflowByBucket);
        }

        // Self-check บริสุทธิ์ — เรียกจาก console harness ชั่วคราวหรือ unit test ในอนาคต (ไม่มี test project ตอนนี้)
        // ตรวจ endWip-startWip == inflow-outflow ต่อแผนก คืนรายการข้อความอธิบาย violation (ว่าง = ผ่านหมด)
        public static List<string> CheckDepartmentInvariant(WipTrend.Response response)
        {
            var violations = new List<string>();
            foreach (var dept in response.Departments)
            {
                var expected = dept.EndWip - dept.StartWip;
                var actual = dept.Inflow - dept.Outflow;
                if (expected != actual)
                {
                    violations.Add(
                        $"{dept.Key}: endWip-startWip={expected} != inflow-outflow={actual} " +
                        $"(startWip={dept.StartWip}, endWip={dept.EndWip}, inflow={dept.Inflow}, outflow={dept.Outflow})");
                }
            }

            return violations;
        }

        private static int FindBucketIndex(IReadOnlyList<DateTime> bucketEnds, DateTime time)
        {
            for (var i = 0; i < bucketEnds.Count; i++)
            {
                if (time <= bucketEnds[i]) return i;
            }

            return -1;
        }

        private static decimal? PercentDelta(int start, int end)
        {
            return start > 0 ? Math.Round((decimal)(end - start) / start * 100, 2) : (decimal?)null;
        }

        // ---- ประกอบ response เต็มของ ProductionInsight/WipTrend ----

        public static WipTrend.Response EvaluateTrend(
            IReadOnlyList<PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers,
            Func<int, string?> departmentKeyOf,
            IReadOnlyList<(string Key, int[] StatusIds)> departments,
            DateTime start,
            DateTime end,
            string bucket)
        {
            var bucketDefs = GenerateBuckets(start, end, bucket);
            var bucketEnds = bucketDefs.Select(b => b.End).ToList();
            var endTime = bucketEnds[bucketEnds.Count - 1];

            var times = new List<DateTime> { start };
            times.AddRange(bucketEnds);

            var wipByTime = ComputeWipCountsAtTimes(plans, headers, departmentKeyOf, times);
            var (flowTotals, flowByBucket) = ComputeBucketedFlow(plans, headers, departmentKeyOf, start, bucketEnds);

            var response = new WipTrend.Response
            {
                Buckets = bucketDefs.Select(b => new WipTrend.BucketInfo { End = b.End, Label = b.Label }).ToList()
            };

            foreach (var dept in departments)
            {
                var startWip = wipByTime[start].ByDept.GetValueOrDefault(dept.Key, 0);
                var endWip = wipByTime[endTime].ByDept.GetValueOrDefault(dept.Key, 0);
                var flow = flowTotals.TryGetValue(dept.Key, out var f) ? f : (Inflow: 0, Outflow: 0);

                var series = new List<WipTrend.SeriesPoint>();
                for (var i = 0; i < bucketEnds.Count; i++)
                {
                    var bucketFlow = flowByBucket[i].TryGetValue(dept.Key, out var bf) ? bf : (Inflow: 0, Outflow: 0);
                    series.Add(new WipTrend.SeriesPoint
                    {
                        BucketEnd = bucketEnds[i],
                        Wip = wipByTime[bucketEnds[i]].ByDept.GetValueOrDefault(dept.Key, 0),
                        Inflow = bucketFlow.Inflow,
                        Outflow = bucketFlow.Outflow
                    });
                }

                response.Departments.Add(new WipTrend.DepartmentTrendData
                {
                    Key = dept.Key,
                    StartWip = startWip,
                    EndWip = endWip,
                    Delta = endWip - startWip,
                    DeltaPercent = PercentDelta(startWip, endWip),
                    Inflow = flow.Inflow,
                    Outflow = flow.Outflow,
                    Net = flow.Inflow - flow.Outflow,
                    Series = series
                });
            }

            var totalStartWip = wipByTime[start].Total;
            var totalEndWip = wipByTime[endTime].Total;

            // total ไม่ใช่ sum ของ department flow (ดู comment บน ComputeTotalCreateExitFlow) — นิยามแยก:
            // inflow = แผนที่ถือกำเนิดในช่วง, outflow = แผนที่ออกจาก WIP ในช่วง
            var (totalInflow, totalOutflow, totalInflowByBucket, totalOutflowByBucket) =
                ComputeTotalCreateExitFlow(plans, headers, start, bucketEnds);

            var totalSeries = new List<WipTrend.SeriesPoint>();
            for (var i = 0; i < bucketEnds.Count; i++)
            {
                totalSeries.Add(new WipTrend.SeriesPoint
                {
                    BucketEnd = bucketEnds[i],
                    Wip = wipByTime[bucketEnds[i]].Total,
                    Inflow = totalInflowByBucket[i],
                    Outflow = totalOutflowByBucket[i]
                });
            }

            response.Total = new WipTrend.TrendSummary
            {
                StartWip = totalStartWip,
                EndWip = totalEndWip,
                Delta = totalEndWip - totalStartWip,
                DeltaPercent = PercentDelta(totalStartWip, totalEndWip),
                Inflow = totalInflow,
                Outflow = totalOutflow,
                Net = totalInflow - totalOutflow,
                Series = totalSeries
            };

            return response;
        }
    }
}
