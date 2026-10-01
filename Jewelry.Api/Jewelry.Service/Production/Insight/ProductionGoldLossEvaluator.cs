using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับข้อมูลที่โหลดมาแล้วเข้ามาเป็น argument (unit-testable)
    //
    // Sign convention (ยืนยันจากโค้ดจริงแล้ว — ดู report):
    // - ช่างแต่ง (tbt_gold_loss_tang_slip): DiffLoss = AllowedLoss - RawLoss → บวก = เสียน้อยกว่าที่ให้ (ดี),
    //   ลบ = เสียเกิน (แย่). TotalMoneyDiff = round(DiffLoss,2) × PricePerGram (sign เดียวกัน)
    // - ช่างฝัง (tbt_worker_gold_loss_slip_item): WeightLossActual ชื่อฟิลด์ทำให้เข้าใจผิดได้ — แท้จริงคือ
    //   "Allowed - RawLoss" สูตรเดียวกับ DiffLoss ของช่างแต่งเป๊ะ (ดู WorkerService.cs:261-264 — RawLoss ไม่ได้
    //   เก็บเป็นฟิลด์แยก ต้องคำนวณเอง = WeightLossAllowed - WeightLossActual). MoneyDiff sign เดียวกัน
    // ดังนั้น "ส่วนเกิน" (over allowance) ของทั้งสองประเภท = max(-diffField, 0) เสมอ
    //
    // Metal (รอบแก้ไข 2026-10-01 — prod พบใบปนเงิน/ทอง ราคาต่างกันมาก 40 บาท/ก. เงิน vs ~1,150-2,300 บาท/ก. ทอง):
    // - setting: จัดประเภทต่อ item ตรงๆ จาก item.Gold ("SV" = เงิน, อื่นๆ = ทอง)
    // - tang: สถิติเก็บที่ระดับ "ใบ" (slip) ไม่ใช่ item จึงต้องสรุปโลหะของทั้งใบจาก items ของมัน — ถ้า item
    //   ทั้งหมดเป็นโลหะเดียวกัน ใช้โลหะนั้นตรงๆ, ถ้าปนกันใช้ "ส่วนใหญ่โดยน้ำหนัก" (sum น้ำหนักต่อกลุ่มโลหะ เทียบกัน)
    //   และตั้ง IsMixedMetal=true ไว้โปร่งใส, ถ้าไม่มี item เลย fallback ดูจาก price_per_gram < 500 (เงินถูกกว่า
    //   ทองมาก ไม่มีทางราคาทองต่ำขนาดนี้)
    public static class ProductionGoldLossEvaluator
    {
        public const int WorkerTypeTang = 50;
        public const int WorkerTypeSetting = 80;

        public const string MetalGold = "GOLD";
        public const string MetalSilver = "SILVER";

        // "SV" = เงิน (silver) — รหัสอื่นทั้งหมดถือเป็นทอง (null/ว่าง ก็ถือเป็นทองไปด้วย — เป็นส่วนใหญ่ของข้อมูล)
        public static string ClassifyMetal(string? goldCode)
        {
            return string.Equals(goldCode?.Trim(), "SV", StringComparison.OrdinalIgnoreCase) ? MetalSilver : MetalGold;
        }

        public class TangSlipItemMetalRow
        {
            public string? Gold { get; set; }
            public decimal? WeightSend { get; set; }
            public decimal? WeightCheck { get; set; }
        }

        // สรุปโลหะของทั้งใบ — ดู comment บนคลาสสำหรับอัลกอริทึม
        public static (string Metal, bool IsMixed) ClassifySlipMetal(IReadOnlyList<TangSlipItemMetalRow> items, decimal? pricePerGram)
        {
            if (items == null || items.Count == 0)
            {
                var fallback = pricePerGram.HasValue && pricePerGram.Value > 0 && pricePerGram.Value < 500 ? MetalSilver : MetalGold;
                return (fallback, false);
            }

            var classified = items.Select(i => (Metal: ClassifyMetal(i.Gold), Weight: i.WeightSend ?? i.WeightCheck ?? 0)).ToList();
            var distinctMetals = classified.Select(c => c.Metal).Distinct().ToList();

            if (distinctMetals.Count == 1)
            {
                return (distinctMetals[0], false);
            }

            var byMetal = classified
                .GroupBy(c => c.Metal)
                .Select(g => (Metal: g.Key, Weight: g.Sum(x => x.Weight)))
                .OrderByDescending(x => x.Weight)
                .ToList();

            return (byMetal[0].Metal, true);
        }

        public class TangSlipRow
        {
            public long SlipId { get; set; }
            public string DocumentNo { get; set; } = null!;
            public string WorkerCode { get; set; } = null!;
            public string? WorkerName { get; set; }
            public DateTime? RequestDateStart { get; set; }
            public DateTime? RequestDateEnd { get; set; }
            public decimal? ReturnedTotal { get; set; }
            public decimal? RawLoss { get; set; }
            public decimal? AllowedLoss { get; set; }
            public decimal? PricePerGram { get; set; }
            public decimal? TotalMoneyDiff { get; set; }
            public List<TangSlipItemMetalRow> Items { get; set; } = new List<TangSlipItemMetalRow>();
        }

        public class SettingSlipItemRow
        {
            public long SlipId { get; set; }
            public string DocumentNo { get; set; } = null!;
            public string WorkerCode { get; set; } = null!;
            public string? WorkerName { get; set; }
            public DateTime RequestDateStart { get; set; }
            public DateTime RequestDateEnd { get; set; }
            public string? Gold { get; set; }
            public decimal? GoldWeightCheck { get; set; }
            public decimal? WeightLossAllowed { get; set; }

            // ชื่อฟิลด์ตาม DB เดิม — ค่าจริงคือ "Allowed - RawLoss" ไม่ใช่ raw loss ตรงๆ (ดู comment บนคลาส)
            public decimal? WeightLossActual { get; set; }
            public decimal? GoldLossPrice { get; set; }
            public decimal? MoneyDiff { get; set; }
        }

        // หน่วยกลางที่ normalize แล้ว — 1 แถว = 1 slip (tang) หรือ 1 item (setting)
        public class LossUnit
        {
            public long SlipId { get; set; }
            public string DocumentNo { get; set; } = null!;
            public int WorkerType { get; set; }
            public string WorkerCode { get; set; } = null!;
            public string? WorkerName { get; set; }

            // 'GOLD' | 'SILVER'
            public string Metal { get; set; } = null!;

            // true เฉพาะ tang ที่ items ปนโลหะกัน (สรุปเป็นโลหะข้างมากแล้วแต่ไม่แน่ใจ 100%) — setting เป็น false เสมอ
            public bool IsMixedMetal { get; set; }

            // ใช้ bucket ตาม request_date_end เสมอ (ทั้ง tang/setting) — BucketStartDate เก็บไว้เพื่อแสดงผลเท่านั้น
            public DateTime BucketDate { get; set; }
            public DateTime? BucketStartDate { get; set; }
            public decimal ReceivedGram { get; set; }
            public decimal RawLossGram { get; set; }
            public decimal AllowedGram { get; set; }
            public decimal? Price { get; set; }

            // signed, sign เดียวกับ DiffLoss/WeightLossActual ต้นทาง (+ = ดี, - = เกิน)
            public decimal MoneyDiff { get; set; }
        }

        public static List<LossUnit> NormalizeTang(IReadOnlyList<TangSlipRow> slips)
        {
            return slips
                .Where(s => s.RequestDateEnd.HasValue)
                .Select(s =>
                {
                    var (metal, isMixed) = ClassifySlipMetal(s.Items, s.PricePerGram);
                    return new LossUnit
                    {
                        SlipId = s.SlipId,
                        DocumentNo = s.DocumentNo,
                        WorkerType = WorkerTypeTang,
                        WorkerCode = s.WorkerCode,
                        WorkerName = s.WorkerName,
                        Metal = metal,
                        IsMixedMetal = isMixed,
                        BucketDate = s.RequestDateEnd!.Value,
                        BucketStartDate = s.RequestDateStart,
                        ReceivedGram = s.ReturnedTotal ?? 0,
                        RawLossGram = s.RawLoss ?? 0,
                        AllowedGram = s.AllowedLoss ?? 0,
                        Price = s.PricePerGram,
                        MoneyDiff = s.TotalMoneyDiff ?? 0
                    };
                })
                .ToList();
        }

        public static List<LossUnit> NormalizeSetting(IReadOnlyList<SettingSlipItemRow> items)
        {
            return items.Select(i =>
            {
                var allowed = i.WeightLossAllowed ?? 0;
                var diff = i.WeightLossActual ?? 0;
                var raw = allowed - diff;

                return new LossUnit
                {
                    SlipId = i.SlipId,
                    DocumentNo = i.DocumentNo,
                    WorkerType = WorkerTypeSetting,
                    WorkerCode = i.WorkerCode,
                    WorkerName = i.WorkerName,
                    Metal = ClassifyMetal(i.Gold),
                    IsMixedMetal = false,
                    BucketDate = i.RequestDateEnd,
                    BucketStartDate = i.RequestDateStart,
                    ReceivedGram = i.GoldWeightCheck ?? 0,
                    RawLossGram = raw,
                    AllowedGram = allowed,
                    Price = i.GoldLossPrice,
                    MoneyDiff = i.MoneyDiff ?? 0
                };
            }).ToList();
        }

        public class KpiResult
        {
            public int WorkerType { get; set; }
            public string Metal { get; set; } = null!;
            public int SlipCount { get; set; }
            public int WorkerCount { get; set; }
            public decimal ReceivedGram { get; set; }
            public decimal RawLossGram { get; set; }
            public decimal AllowedGram { get; set; }
            public decimal? LossPercent { get; set; }
            public decimal? AllowedPercent { get; set; }
            public decimal TargetPercent { get; set; }
            public decimal ExcessGram { get; set; }
            public decimal ExcessMoney { get; set; }
            public decimal NetMoney { get; set; }
            public int OverCount { get; set; }
            public int WorkersOverCount { get; set; }
            public int CoverageJobs { get; set; }
            public int CoverageTotalJobs { get; set; }
            public decimal? CoveragePercent { get; set; }
        }

        public static KpiResult ComputeKpi(
            int workerType, string metal, IReadOnlyList<LossUnit> units, decimal targetPercent, int coverageJobs, int coverageTotalJobs)
        {
            var receivedGram = units.Sum(u => u.ReceivedGram);
            var rawLossGram = units.Sum(u => u.RawLossGram);
            var allowedGram = units.Sum(u => u.AllowedGram);
            var excessGram = units.Sum(u => Math.Max(u.RawLossGram - u.AllowedGram, 0));
            var excessMoney = units.Sum(u => Math.Max(u.RawLossGram - u.AllowedGram, 0) * (u.Price ?? 0));
            var netMoney = units.Sum(u => u.MoneyDiff);
            var overCount = units.Count(u => u.RawLossGram > u.AllowedGram);
            var workerGroups = units.GroupBy(u => u.WorkerCode).ToList();
            var workersOverCount = workerGroups.Count(g => g.Sum(u => u.RawLossGram) > g.Sum(u => u.AllowedGram));

            return new KpiResult
            {
                WorkerType = workerType,
                Metal = metal,
                SlipCount = units.Count,
                WorkerCount = workerGroups.Count,
                ReceivedGram = Round(receivedGram),
                RawLossGram = Round(rawLossGram),
                AllowedGram = Round(allowedGram),
                LossPercent = receivedGram > 0 ? Round(rawLossGram / receivedGram * 100, 3) : (decimal?)null,
                AllowedPercent = receivedGram > 0 ? Round(allowedGram / receivedGram * 100, 3) : (decimal?)null,
                TargetPercent = targetPercent,
                ExcessGram = Round(excessGram),
                ExcessMoney = Round(excessMoney, 2),
                NetMoney = Round(netMoney, 2),
                OverCount = overCount,
                WorkersOverCount = workersOverCount,
                CoverageJobs = coverageJobs,
                CoverageTotalJobs = coverageTotalJobs,
                CoveragePercent = coverageTotalJobs > 0 ? Round((decimal)coverageJobs / coverageTotalJobs * 100, 1) : (decimal?)null
            };
        }

        public class SeriesResult
        {
            public DateTime BucketEnd { get; set; }
            public int WorkerType { get; set; }
            public int SlipCount { get; set; }
            public decimal? RawLossGram { get; set; }
            public decimal? AllowedGram { get; set; }
            public decimal? LossPercent { get; set; }
            public decimal? AllowedPercent { get; set; }
        }

        public static List<SeriesResult> ComputeSeries(
            int workerType, IReadOnlyList<LossUnit> units, DateTime start, IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new List<SeriesResult>();
            var prev = start;

            foreach (var bucketEnd in bucketEnds)
            {
                var bucketUnits = units.Where(u => u.BucketDate > prev && u.BucketDate <= bucketEnd).ToList();

                if (bucketUnits.Count == 0)
                {
                    result.Add(new SeriesResult
                    {
                        BucketEnd = bucketEnd,
                        WorkerType = workerType,
                        SlipCount = 0,
                        RawLossGram = null,
                        AllowedGram = null,
                        LossPercent = null,
                        AllowedPercent = null
                    });
                }
                else
                {
                    var received = bucketUnits.Sum(u => u.ReceivedGram);
                    var raw = bucketUnits.Sum(u => u.RawLossGram);
                    var allowed = bucketUnits.Sum(u => u.AllowedGram);

                    result.Add(new SeriesResult
                    {
                        BucketEnd = bucketEnd,
                        WorkerType = workerType,
                        SlipCount = bucketUnits.Count,
                        RawLossGram = Round(raw),
                        AllowedGram = Round(allowed),
                        LossPercent = received > 0 ? Round(raw / received * 100, 3) : (decimal?)null,
                        AllowedPercent = received > 0 ? Round(allowed / received * 100, 3) : (decimal?)null
                    });
                }

                prev = bucketEnd;
            }

            return result;
        }

        public class WorkerResult
        {
            public int WorkerType { get; set; }
            public string WorkerCode { get; set; } = null!;
            public string? WorkerName { get; set; }
            public int SlipCount { get; set; }
            public decimal ReceivedGram { get; set; }
            public decimal? LossPercent { get; set; }
            public decimal? AllowedPercent { get; set; }
            public decimal TargetPercent { get; set; }
            public decimal ExcessGram { get; set; }
            public decimal ExcessMoney { get; set; }
            public decimal NetMoney { get; set; }
            public int OverBuckets { get; set; }
            public int QualifyingBuckets { get; set; }
        }

        // qualifying bucket = bucket ที่ worker คนนี้มี slip/item อย่างน้อย 1 รายการ — over bucket = bucket นั้น
        // rawLossGram รวม > allowedGram รวม (เทียบ allowance ของตัวเอง A ไม่ใช่เป้าหมายบริษัท B)
        public static List<WorkerResult> ComputeWorkers(
            int workerType, IReadOnlyList<LossUnit> units, decimal targetPercent, DateTime start, IReadOnlyList<DateTime> bucketEnds)
        {
            var result = new List<WorkerResult>();

            foreach (var g in units.GroupBy(u => u.WorkerCode))
            {
                var list = g.ToList();
                var received = list.Sum(u => u.ReceivedGram);
                var raw = list.Sum(u => u.RawLossGram);
                var allowed = list.Sum(u => u.AllowedGram);
                var excessGram = list.Sum(u => Math.Max(u.RawLossGram - u.AllowedGram, 0));
                var excessMoney = list.Sum(u => Math.Max(u.RawLossGram - u.AllowedGram, 0) * (u.Price ?? 0));

                var qualifyingBuckets = 0;
                var overBuckets = 0;
                var prev = start;
                foreach (var bucketEnd in bucketEnds)
                {
                    var bucketUnits = list.Where(u => u.BucketDate > prev && u.BucketDate <= bucketEnd).ToList();
                    if (bucketUnits.Count > 0)
                    {
                        qualifyingBuckets++;
                        var bRaw = bucketUnits.Sum(u => u.RawLossGram);
                        var bAllowed = bucketUnits.Sum(u => u.AllowedGram);
                        if (bRaw > bAllowed) overBuckets++;
                    }
                    prev = bucketEnd;
                }

                result.Add(new WorkerResult
                {
                    WorkerType = workerType,
                    WorkerCode = g.Key,
                    WorkerName = list.Select(u => u.WorkerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    SlipCount = list.Count,
                    ReceivedGram = Round(received),
                    LossPercent = received > 0 ? Round(raw / received * 100, 3) : (decimal?)null,
                    AllowedPercent = received > 0 ? Round(allowed / received * 100, 3) : (decimal?)null,
                    TargetPercent = targetPercent,
                    ExcessGram = Round(excessGram),
                    ExcessMoney = Round(excessMoney, 2),
                    NetMoney = Round(list.Sum(u => u.MoneyDiff), 2),
                    OverBuckets = overBuckets,
                    QualifyingBuckets = qualifyingBuckets
                });
            }

            return result;
        }

        private static decimal Round(decimal value, int digits = 2) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }
}
