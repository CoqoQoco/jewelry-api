using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับข้อมูลที่โหลด/คำนวณมาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // Done = transfer แรกเข้า status 100 (tbt_production_plan_transfer_status.target_status=100) หรือ fallback
    // plan.completed_date ถ้าไม่มี row transfer เลย (ดู IProductionDeliveryDataProvider.GetDoneDatesAsync)
    // On-time = เทียบ "วันที่ไทย" (AddHours(7).Date) ของ done กับ request_date — ไม่ใช่ UTC instant ตรงๆ
    public static class ProductionDeliveryEvaluator
    {
        public class CompletedPlanRow
        {
            public string? CustomerNumber { get; set; }
            public DateTime CreateDate { get; set; }
            public DateTime RequestDate { get; set; }
            public DateTime DoneDate { get; set; }
        }

        public class OnTimeStats
        {
            public int CompletedCount { get; set; }
            public int OnTimeCount { get; set; }
            public decimal? OnTimePercent { get; set; }
            public double? LateMedianDays { get; set; }
            public double? PlannedLeadMedianDays { get; set; }
            public double? ActualLeadMedianDays { get; set; }
        }

        public static bool IsOnTimeThai(DateTime doneUtc, DateTime requestUtc)
        {
            return doneUtc.AddHours(7).Date <= requestUtc.AddHours(7).Date;
        }

        public static OnTimeStats ComputeOnTimeStats(IReadOnlyList<CompletedPlanRow> completed)
        {
            if (completed.Count == 0)
            {
                return new OnTimeStats
                {
                    CompletedCount = 0,
                    OnTimeCount = 0,
                    OnTimePercent = null,
                    LateMedianDays = null,
                    PlannedLeadMedianDays = null,
                    ActualLeadMedianDays = null
                };
            }

            var onTimeCount = 0;
            var lateDays = new List<double>();
            var plannedLead = new List<double>();
            var actualLead = new List<double>();

            foreach (var p in completed)
            {
                var onTime = IsOnTimeThai(p.DoneDate, p.RequestDate);
                if (onTime)
                {
                    onTimeCount++;
                }
                else
                {
                    lateDays.Add((p.DoneDate.AddHours(7).Date - p.RequestDate.AddHours(7).Date).TotalDays);
                }

                plannedLead.Add((p.RequestDate - p.CreateDate).TotalDays);
                actualLead.Add((p.DoneDate - p.CreateDate).TotalDays);
            }

            return new OnTimeStats
            {
                CompletedCount = completed.Count,
                OnTimeCount = onTimeCount,
                OnTimePercent = Math.Round((decimal)onTimeCount / completed.Count * 100, 1),
                LateMedianDays = lateDays.Count > 0 ? Round1(ProductionStageLeadTimeEvaluator.Median(lateDays)) : (double?)null,
                PlannedLeadMedianDays = Round1(ProductionStageLeadTimeEvaluator.Median(plannedLead)),
                ActualLeadMedianDays = Round1(ProductionStageLeadTimeEvaluator.Median(actualLead))
            };
        }

        // suggestedLeadDays = ceil(actual lead median) — ถ้าไม่มีข้อมูล (ไม่มีแผนเสร็จเลย) คืน 0
        public static int SuggestedLeadDays(double? actualLeadMedianDays)
        {
            return actualLeadMedianDays.HasValue ? (int)Math.Ceiling(actualLeadMedianDays.Value) : 0;
        }

        // แบ่ง bucket ตาม DoneDate (ไม่ใช่ CreateDate) — สอดคล้องกับนิยาม "completed-in-range" ของสเปก
        public static List<(DateTime BucketEnd, OnTimeStats Stats)> ComputeSeries(
            IReadOnlyList<CompletedPlanRow> allCompleted,
            DateTime start,
            IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new List<(DateTime, OnTimeStats)>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var bucketItems = allCompleted.Where(p => p.DoneDate > prev && p.DoneDate <= bucketEnd).ToList();
                result.Add((bucketEnd, ComputeOnTimeStats(bucketItems)));
                prev = bucketEnd;
            }

            return result;
        }

        public class CustomerDeliveryRow
        {
            public string CustomerCode { get; set; } = null!;
            public int CompletedCount { get; set; }
            public int LateCount { get; set; }
            public decimal LatePercent { get; set; }
            public double? LateMedianDays { get; set; }
        }

        // top 10 เรียงตาม lateCount มากสุด — เฉพาะลูกค้าที่มีอย่างน้อย 1 งานส่งช้า
        public static List<CustomerDeliveryRow> ComputeLateCustomers(IReadOnlyList<CompletedPlanRow> completed)
        {
            return completed
                .Where(p => !string.IsNullOrWhiteSpace(p.CustomerNumber))
                .GroupBy(p => p.CustomerNumber!)
                .Select(g =>
                {
                    var items = g.ToList();
                    var lateItems = items.Where(p => !IsOnTimeThai(p.DoneDate, p.RequestDate)).ToList();
                    var lateDays = lateItems
                        .Select(p => (p.DoneDate.AddHours(7).Date - p.RequestDate.AddHours(7).Date).TotalDays)
                        .ToList();

                    return new CustomerDeliveryRow
                    {
                        CustomerCode = g.Key,
                        CompletedCount = items.Count,
                        LateCount = lateItems.Count,
                        LatePercent = items.Count > 0 ? Math.Round((decimal)lateItems.Count / items.Count * 100, 1) : 0,
                        LateMedianDays = lateDays.Count > 0 ? Round1(ProductionStageLeadTimeEvaluator.Median(lateDays)) : (double?)null
                    };
                })
                .Where(c => c.LateCount > 0)
                .OrderByDescending(c => c.LateCount)
                .Take(10)
                .ToList();
        }

        // median(เวลาที่ visit ออกจากแผนกบัตรต้นทุน -> ถึง Done จริง) — ต้อง exited แล้วและมี DoneDate ทั้งคู่
        // ค่าติดลบ (done ก่อนออกจาก costCard — ข้อมูลผิดปกติ/เวลาสองตารางไม่ล็อกกันสนิท) ถูกข้ามทิ้งไม่นับ
        public static double? ComputeCostCardExitToDoneMedian(
            IReadOnlyList<ProductionStageLeadTimeEvaluator.VisitRecord> allVisits,
            IReadOnlyDictionary<int, DateTime> doneDates)
        {
            var gaps = new List<double>();

            foreach (var visit in allVisits)
            {
                if (visit.DeptKey != "costCard" || !visit.End.HasValue) continue;
                if (!doneDates.TryGetValue(visit.PlanId, out var doneDate)) continue;

                var gapDays = (doneDate - visit.End.Value).TotalDays;
                if (gapDays < 0) continue;

                gaps.Add(gapDays);
            }

            return gaps.Count > 0 ? Round1(ProductionStageLeadTimeEvaluator.Median(gaps)) : (double?)null;
        }

        public class AtRiskProjection
        {
            public DateTime ProjectedFinishDate { get; set; }
            public double ProjectedLateDays { get; set; }
            public double RemainingDays { get; set; }
        }

        // projectedFinish = now + max(0, median(currentDept) - daysInCurrentDept)
        //                       + Σ median(แผนกถัดไปตามลำดับคงที่ design->trim->rawPolish->gemSort->setting->plating->costCard)
        //                       + median(costCard exit -> done) (ถ้าคำนวณได้ — ไม่งั้นถือเป็น 0)
        // Simplification (ตามที่สเปกอนุญาต): ใช้ median รวมของทั้งแผนก (รวมรอ+ทำงาน) ไม่ได้ตามแผนจริงทีละตัว
        // ไม่ได้หักลบเวลาที่ "รอ" อยู่ในแผนกถัดไปจริงจากคิวงานปัจจุบัน — ประมาณการหยาบระดับแดชบอร์ด
        public static AtRiskProjection ComputeAtRiskProjection(
            string? currentDeptKey,
            double daysInCurrentDept,
            DateTime requestDate,
            DateTime now,
            IReadOnlyList<(string Key, int[] StatusIds)> departments,
            IReadOnlyDictionary<string, double> medianTotalByDept,
            double costCardExitToDoneMedianDays)
        {
            double remaining = 0;

            if (currentDeptKey != null)
            {
                var idx = -1;
                for (var i = 0; i < departments.Count; i++)
                {
                    if (departments[i].Key == currentDeptKey) { idx = i; break; }
                }

                if (idx >= 0)
                {
                    var currentMedian = medianTotalByDept.TryGetValue(currentDeptKey, out var cm) ? cm : 0;
                    remaining += Math.Max(0, currentMedian - daysInCurrentDept);

                    for (var i = idx + 1; i < departments.Count; i++)
                    {
                        remaining += medianTotalByDept.TryGetValue(departments[i].Key, out var m) ? m : 0;
                    }
                }
            }

            remaining += Math.Max(0, costCardExitToDoneMedianDays);

            var projectedFinish = now.AddDays(remaining);
            var projectedLateDays = (projectedFinish.AddHours(7).Date - requestDate.AddHours(7).Date).TotalDays;

            return new AtRiskProjection
            {
                ProjectedFinishDate = projectedFinish,
                ProjectedLateDays = Round1(projectedLateDays),
                RemainingDays = Round1(remaining)
            };
        }

        private static double Round1(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}
