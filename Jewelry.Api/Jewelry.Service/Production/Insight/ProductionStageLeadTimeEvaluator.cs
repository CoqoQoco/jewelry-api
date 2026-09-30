using System;
using System.Collections.Generic;
using System.Linq;
using StageLeadTime = jewelry.Model.Production.Insight.StageLeadTime;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับ plan/header rows (เหมือน ProductionPlanTrendEvaluator ใช้) เข้ามาเป็น argument
    //
    // Visit = ช่วงที่แผนอยู่ในแผนกใดแผนกหนึ่งต่อเนื่อง
    // - visit start = header แรกที่เข้าแผนก (สำหรับออกแบบ: create_date ถ้ายังไม่มี header เลย — synthetic entry
    //   จาก ProductionPlanTrendEvaluator.BuildTimelines อยู่แล้ว)
    // - visit end = entry แรกที่ "ไม่ใช่" แผนกเดิม (ย้ายแผนกอื่น, สถานะที่ไม่อยู่แผนกไหนเลยเช่น 84/85 CVD,
    //   หรือออกจาก WIP ไปเลย 100/500/synthetic exit)
    //
    // Round 2 — แยก wait/work จาก receive_date จริง (Round 1 ผิด — เดา wait/work จาก "ซึ่ง status ของ header
    // ตัวเอง" ทั้งที่ header ไม่เคยถูกสร้างด้วยสถานะรอเลยสักครั้ง — ยืนยันจากข้อมูล prod จริง):
    // สถานะ "รอ" (49/59/69/79/89/94) ไม่เคยถูกเขียนเป็น header แยกต่างหาก — header ของแผนกที่ไม่ใช่ออกแบบจะมี
    // Status = สถานะ "ทำงาน" (50/60/70/80/90/95) เสมอตั้งแต่สร้าง (ตอนโอนงานเข้าแผนก) — receive_date (คอลัมน์ใหม่)
    // คือเวลาที่ plan.Status พ้นสถานะรอจริง (บันทึกตอนแผนกกดเริ่มงาน) ต่างหาก
    // - มี receive_date: wait = [header.CreateDate, ReceiveDate), work = [ReceiveDate, visit end) — HasSplit=true
    // - ไม่มี receive_date, exited แล้ว: แยกไม่ได้ — HasSplit=false, WaitDays/WorkDays=null (TotalDays ยังรู้เสมอ)
    // - ออกแบบ (10): ไม่มีสถานะรอเลยในนิยาม — ทั้ง visit = work ล้วน HasSplit=true เสมอ
    // - ongoing (ยังไม่จบ) ไม่มี receive_date แต่ plan.Status ปัจจุบัน = สถานะรอของแผนกนี้พอดี → แปลว่ายังไม่เคย
    //   รับงานเลยจนถึงตอนนี้แน่นอน (รู้ชัด) → HasSplit=true, รอมาทั้งหมดตั้งแต่เข้าแผนก, WorkDays=0
    // - ongoing อื่นๆ ที่ไม่มี receive_date (ข้อมูลเก่าก่อน migration ที่ plan.Status ข้ามพ้นสถานะรอไปแล้วแต่ไม่มี
    //   บันทึกเวลา) → HasSplit=false เช่นกัน
    public static class ProductionStageLeadTimeEvaluator
    {
        public class VisitRecord
        {
            public int PlanId { get; set; }
            public string DeptKey { get; set; } = null!;
            public DateTime Start { get; set; }
            public DateTime? End { get; set; }
            public double TotalDays { get; set; }

            // false = แยก wait/work ไม่ได้ (ไม่มี receive_date และไม่ใช่กรณีที่รู้ชัดว่ายังรออยู่) — WaitDays/WorkDays เป็น null
            public bool HasSplit { get; set; }
            public double? WaitDays { get; set; }
            public double? WorkDays { get; set; }
        }

        public static List<VisitRecord> ComputeVisits(
            IReadOnlyList<ProductionPlanTrendEvaluator.PlanTrendRow> plans,
            IReadOnlyList<ProductionPlanFlowCalculator.HeaderRow> headers,
            IReadOnlyList<(string Key, int[] StatusIds)> departments,
            DateTime now)
        {
            var timelines = ProductionPlanTrendEvaluator.BuildTimelines(plans, headers);
            var visits = new List<VisitRecord>();

            string? DeptOf(int status)
            {
                foreach (var d in departments)
                {
                    if (Array.IndexOf(d.StatusIds, status) >= 0) return d.Key;
                }
                return null;
            }

            // สถานะ "รอ" ของแผนก (index 0 ของ StatusIds) — null ถ้าแผนกนี้ไม่มีสถานะรอ (ออกแบบ)
            int? WaitStatusOf(string deptKey)
            {
                var d = departments.First(x => x.Key == deptKey);
                return d.StatusIds.Length >= 2 ? d.StatusIds[0] : (int?)null;
            }

            foreach (var plan in plans)
            {
                var timeline = timelines[plan.Id];
                var i = 0;

                while (i < timeline.Count)
                {
                    var deptKey = DeptOf(timeline[i].Status);
                    if (deptKey == null)
                    {
                        i++;
                        continue;
                    }

                    var visitStart = timeline[i].Time;
                    var receiveDate = timeline[i].ReceiveDate;
                    var isDesign = deptKey == "design";

                    // ในทางปฏิบัติแผนกที่ไม่ใช่ออกแบบจะมีแค่ header เดียวต่อ 1 visit เสมอ (ไม่มี header สถานะรอ
                    // ให้ "ต่อ" กัน) — loop นี้เผื่อไว้เฉยๆ ถ้าโครงสร้างข้อมูลเปลี่ยนในอนาคต
                    var k = i + 1;
                    while (k < timeline.Count && DeptOf(timeline[k].Status) == deptKey)
                    {
                        k++;
                    }

                    DateTime? visitEnd = k < timeline.Count ? timeline[k].Time : (DateTime?)null;
                    var totalDays = visitEnd.HasValue
                        ? (visitEnd.Value - visitStart).TotalDays
                        : Math.Max((now - visitStart).TotalDays, 0);

                    bool hasSplit;
                    double? waitDays;
                    double? workDays;

                    if (isDesign)
                    {
                        hasSplit = true;
                        waitDays = 0;
                        workDays = totalDays;
                    }
                    else if (receiveDate.HasValue)
                    {
                        hasSplit = true;
                        var w = Math.Max((receiveDate.Value - visitStart).TotalDays, 0);
                        waitDays = w;
                        workDays = Math.Max(totalDays - w, 0);
                    }
                    else if (!visitEnd.HasValue && WaitStatusOf(deptKey) == plan.Status)
                    {
                        // ยัง ongoing และ plan.Status ปัจจุบัน = สถานะรอของแผนกนี้พอดี → รอมาตั้งแต่เข้าแผนกทั้งหมด (รู้ชัด)
                        hasSplit = true;
                        waitDays = totalDays;
                        workDays = 0;
                    }
                    else
                    {
                        // แยกไม่ได้ — ไม่มี receive_date และไม่เข้าเงื่อนไขที่รู้ชัดว่ายังรออยู่
                        hasSplit = false;
                        waitDays = null;
                        workDays = null;
                    }

                    visits.Add(new VisitRecord
                    {
                        PlanId = plan.Id,
                        DeptKey = deptKey,
                        Start = visitStart,
                        End = visitEnd,
                        TotalDays = totalDays,
                        HasSplit = hasSplit,
                        WaitDays = waitDays,
                        WorkDays = workDays
                    });

                    // visit จบแล้ว (exited) → k ชี้ entry ที่ออกจากแผนกไปแล้ว สแกนต่อจากตรงนั้น (อาจเป็นจุดเริ่ม
                    // visit ใหม่ของแผนกอื่นทันที) — visit ยังไม่จบ (ongoing) → k == timeline.Count จบ loop เอง
                    i = k;
                }
            }

            return visits;
        }

        // ---- สถิติต่อแผนก (ใช้ทั้ง endpoint StageLeadTime และกฎใน Wip) ----
        // stalePlanIds: แผนที่ "ค้างนาน" (นิยามเดียวกับ WIP_STALE) ไม่นับใน AbnormalCount (ถูกคัดกรองไว้ที่
        // StalePlans table อยู่แล้ว ไม่อยากให้ปนกัน) — CurrentCount ไม่ตัด (เป็นแค่ snapshot จำนวนที่อยู่แผนกนี้)
        public static StageLeadTime.DepartmentLeadTimeData BuildDepartmentStats(
            IReadOnlyList<VisitRecord> deptVisits,
            decimal standardDays,
            string standardSource,
            DateTime? standardEffectiveFrom,
            DateTime start,
            DateTime end,
            IReadOnlyList<DateTime> bucketEnds,
            DateTime now,
            decimal abnormalMultiplier,
            int currentWaitingCount,
            IReadOnlySet<int> stalePlanIds)
        {
            var exited = deptVisits.Where(v => v.End.HasValue && v.End.Value >= start && v.End.Value <= end).ToList();
            var ongoing = deptVisits.Where(v => !v.End.HasValue).ToList();
            var exitedSplit = exited.Where(v => v.HasSplit).ToList();

            var totalList = exited.Select(v => v.TotalDays).ToList();
            var waitList = exitedSplit.Select(v => v.WaitDays!.Value).ToList();
            var workList = exitedSplit.Select(v => v.WorkDays!.Value).ToList();

            var medianTotal = Round1(Median(totalList));
            double? medianWait = exitedSplit.Count > 0 ? Round1(Median(waitList)) : (double?)null;
            double? medianWork = exitedSplit.Count > 0 ? Round1(Median(workList)) : (double?)null;

            decimal? overStandardPercent = standardDays > 0
                ? Math.Round((decimal)(medianTotal - (double)standardDays) / standardDays * 100, 1)
                : (decimal?)null;

            var abnormalThresholdDays = (double)(standardDays * abnormalMultiplier);
            var abnormalCount = ongoing.Count(v => !stalePlanIds.Contains(v.PlanId) && (now - v.Start).TotalDays > abnormalThresholdDays);

            var series = new List<StageLeadTime.LeadTimeSeriesPoint>();
            var prevBoundary = start;
            foreach (var bucketEnd in bucketEnds)
            {
                var bucketVisits = exited.Where(v => v.End!.Value > prevBoundary && v.End.Value <= bucketEnd).ToList();
                var bucketSplit = bucketVisits.Where(v => v.HasSplit).ToList();

                series.Add(new StageLeadTime.LeadTimeSeriesPoint
                {
                    BucketEnd = bucketEnd,
                    Count = bucketVisits.Count,
                    // count=0 (หรือไม่มี split sample) → null (ไม่ใช่ 0 จริง) กัน UI/กฎวิเคราะห์เข้าใจผิดว่ามีข้อมูลจริง
                    MedianTotal = bucketVisits.Count == 0 ? (double?)null : Round1(Median(bucketVisits.Select(v => v.TotalDays).ToList())),
                    MedianWait = bucketSplit.Count == 0 ? (double?)null : Round1(Median(bucketSplit.Select(v => v.WaitDays!.Value).ToList())),
                    MedianWork = bucketSplit.Count == 0 ? (double?)null : Round1(Median(bucketSplit.Select(v => v.WorkDays!.Value).ToList())),
                    P90Total = bucketVisits.Count == 0 ? (double?)null : Round1(Percentile(bucketVisits.Select(v => v.TotalDays).ToList(), 90))
                });
                prevBoundary = bucketEnd;
            }

            return new StageLeadTime.DepartmentLeadTimeData
            {
                StandardDays = standardDays,
                StandardSource = standardSource,
                StandardEffectiveFrom = standardEffectiveFrom,
                ExitedCount = exited.Count,
                Median = new StageLeadTime.LeadTimeStat { Total = medianTotal, Wait = medianWait, Work = medianWork },
                P90 = new StageLeadTime.LeadTimeStat
                {
                    Total = Round1(Percentile(totalList, 90)),
                    Wait = exitedSplit.Count > 0 ? Round1(Percentile(waitList, 90)) : (double?)null,
                    Work = exitedSplit.Count > 0 ? Round1(Percentile(workList, 90)) : (double?)null
                },
                OverStandardPercent = overStandardPercent,
                CurrentCount = ongoing.Count,
                AbnormalCount = abnormalCount,
                SplitSampleCount = exitedSplit.Count,
                CurrentWaitingCount = currentWaitingCount,
                Series = series
            };
        }

        // ---- Capacity (Little's Law) — Round 2: ใช้อัตรา exit ที่วัดได้จริง ไม่ใช้ WIP ----
        // WIP-based (Round 1) ผิด: WIP รวมแผนค้างนาน (stale) นับพันตัวเข้าไปด้วย ทำให้ throughput = wip/medianTotal
        // เพี้ยนมาก (เช่นออกแบบ 2,074 ตัว / 20 วัน = 103 ตัว/วัน ทั้งที่ exit จริง ≈ 9 ตัว/วัน) — Round 2 วัดจาก
        // exitedCount/rangeDays ตรงๆ (อัตราที่ "ออกจากแผนกจริง" ในช่วงที่ขอ) แทน
        //
        // exitedPerDay = exitedCount / rangeDays (throughput ที่วัดได้จริงในช่วงนี้)
        // atStandardPerDay = ถ้าตอนนี้ช้ากว่า standard (medianTotal > standard) จะเร็วขึ้นตามสัดส่วนถ้าทำได้ตาม
        //   standard (Little's Law, WIP คงที่: L = λW → λ = L/W ใหม่ = exitedPerDay × medianTotal/standard) —
        //   ถ้าเร็วกว่า standard อยู่แล้วหรือไม่มี standard ก็ไม่เปลี่ยน (ใช้ exitedPerDay เดิม)
        // bottleneck = แผนกที่ per-day ต่ำสุด "ในบรรดาแผนกที่มี exit จริงในช่วงนี้" (exitedCount>0) — แผนกที่ไม่มี
        //   exit เลยไม่มีข้อมูลวัด ไม่ควรถูกเข้าใจผิดว่าเป็นคอขวด
        // TotalLeadDays = Σ medianTotal (current) / Σ min(medianTotal, standard) (atStandard) — เวลารวมถ้าแผน
        //   เดินทางผ่านทุกแผนกตามลำดับครั้งละหนึ่งรอบ (สมมติฐานง่ายๆ ไม่ได้ตามแผนจริงทีละตัว)
        // MonthlyThroughput = 30 × bottleneck per-day
        public static StageLeadTime.CapacityData BuildCapacity(
            IReadOnlyList<VisitRecord> allVisits,
            IReadOnlyList<(string Key, int[] StatusIds)> departments,
            IReadOnlyDictionary<string, decimal> standardByDept,
            DateTime start,
            DateTime end)
        {
            var rangeDays = Math.Max((end - start).TotalDays, 1);

            var rows = new List<(string Key, int ExitedCount, double ExitedPerDay, double MedianTotal, double StandardDays, double AtStandardPerDay)>();

            foreach (var dept in departments)
            {
                var exitedTotals = allVisits
                    .Where(v => v.DeptKey == dept.Key && v.End.HasValue && v.End.Value >= start && v.End.Value <= end)
                    .Select(v => v.TotalDays)
                    .ToList();

                var exitedCount = exitedTotals.Count;
                var exitedPerDay = exitedCount / rangeDays;
                var medianTotal = Median(exitedTotals);
                var standard = standardByDept.TryGetValue(dept.Key, out var s) ? (double)s : 0;

                var atStandardPerDay = medianTotal > standard && standard > 0
                    ? exitedPerDay * medianTotal / standard
                    : exitedPerDay;

                rows.Add((dept.Key, exitedCount, exitedPerDay, medianTotal, standard, atStandardPerDay));
            }

            var withExits = rows.Where(r => r.ExitedCount > 0).ToList();

            var bottleneckCurrentKey = withExits.Count > 0
                ? withExits.OrderBy(r => r.ExitedPerDay).First().Key
                : (string?)null;
            var bottleneckAtStandardKey = withExits.Count > 0
                ? withExits.OrderBy(r => r.AtStandardPerDay).First().Key
                : (string?)null;

            var departmentsOut = rows.Select(r => new StageLeadTime.CapacityDepartmentData
            {
                Key = r.Key,
                ExitedCount = r.ExitedCount,
                ExitedPerDay = Round1(r.ExitedPerDay),
                MedianTotal = Round1(r.MedianTotal),
                StandardDays = Round1(r.StandardDays),
                AtStandardPerDay = Round1(r.AtStandardPerDay),
                IsBottleneckCurrent = r.Key == bottleneckCurrentKey,
                IsBottleneckAtStandard = r.Key == bottleneckAtStandardKey
            }).ToList();

            var bottleneckCurrentRow = rows.FirstOrDefault(r => r.Key == bottleneckCurrentKey);
            var bottleneckAtStandardRow = rows.FirstOrDefault(r => r.Key == bottleneckAtStandardKey);

            double CapAt(double medianTotal, double standard) => standard > 0 ? Math.Min(medianTotal, standard) : medianTotal;

            return new StageLeadTime.CapacityData
            {
                Current = new StageLeadTime.CapacitySummary
                {
                    TotalLeadDays = Round1(rows.Sum(r => r.MedianTotal)),
                    MonthlyThroughput = Round1(30 * bottleneckCurrentRow.ExitedPerDay),
                    BottleneckDept = bottleneckCurrentKey
                },
                AtStandard = new StageLeadTime.CapacitySummary
                {
                    TotalLeadDays = Round1(rows.Sum(r => CapAt(r.MedianTotal, r.StandardDays))),
                    MonthlyThroughput = Round1(30 * bottleneckAtStandardRow.AtStandardPerDay),
                    BottleneckDept = bottleneckAtStandardKey
                },
                Departments = departmentsOut
            };
        }

        // ---- helpers (pure, testable) ----

        public static double Median(IReadOnlyList<double> values)
        {
            if (values.Count == 0) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
        }

        // nearest-rank method — พอสำหรับ dashboard ไม่ต้อง interpolate
        public static double Percentile(IReadOnlyList<double> values, double p)
        {
            if (values.Count == 0) return 0;
            var sorted = values.OrderBy(v => v).ToList();
            var rank = (int)Math.Ceiling(p / 100.0 * sorted.Count);
            rank = Math.Max(1, Math.Min(sorted.Count, rank));
            return sorted[rank - 1];
        }

        private static double Round1(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}
