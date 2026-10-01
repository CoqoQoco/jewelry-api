using System;
using System.Collections.Generic;
using System.Linq;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Production.Insight
{
    // Pure — ไม่แตะ DB, รับตัวเลขที่คำนวณมาแล้วเข้ามาเป็น argument (unit-testable)
    public static class ProductionInsightRuleEngine
    {
        public static Wip.Finding? EvaluateWipStale(int count, int openCount)
        {
            if (count <= 0) return null;

            var percent = PercentOf(count, openCount);
            var severity = percent >= ProductionInsightThresholds.WipStalePercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_STALE",
                Severity = severity,
                ReportRef = "stalePlans",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["openCount"] = openCount,
                    ["percent"] = percent
                }
            };
        }

        public static Wip.Finding? EvaluateWipOverdue(int count, int openCount)
        {
            if (count <= 0) return null;

            var percent = PercentOf(count, openCount);
            var severity = percent >= ProductionInsightThresholds.WipOverduePercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_OVERDUE",
                Severity = severity,
                ReportRef = "dueRisk",
                Params = new Dictionary<string, object>
                {
                    ["count"] = count,
                    ["openCount"] = openCount,
                    ["percent"] = percent
                }
            };
        }

        public static Wip.Finding? EvaluateWipDeptStaleTop(IReadOnlyDictionary<string, int> staleByDept, int totalStaleCount)
        {
            if (staleByDept == null || staleByDept.Count == 0) return null;

            var top = staleByDept.OrderByDescending(kv => kv.Value).First();
            if (top.Value <= 0) return null;

            var share = PercentOf(top.Value, totalStaleCount);

            return new Wip.Finding
            {
                Code = "WIP_DEPT_STALE_TOP",
                Severity = "warning",
                ReportRef = "departments",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = top.Key,
                    ["count"] = top.Value,
                    ["share"] = share
                }
            };
        }

        public static Wip.Finding? EvaluateWipMeltedOpen(int count)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "WIP_MELTED_OPEN",
                Severity = "warning",
                ReportRef = "departments",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        public static Wip.Finding? EvaluateFcBecomingStale(int count, int riskWindowDays)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_BECOMING_STALE",
                Severity = "warning",
                ReportRef = "stalePlans",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = riskWindowDays }
            };
        }

        public static Wip.Finding? EvaluateFcDueSoonAtRisk(int count, int riskWindowDays)
        {
            if (count <= 0) return null;

            var severity = count >= ProductionInsightThresholds.FcDueSoonAtRiskCountCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "FC_DUE_SOON_AT_RISK",
                Severity = severity,
                ReportRef = "dueRisk",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = riskWindowDays }
            };
        }

        public static Wip.Finding? EvaluateFcBottleneck(IReadOnlyDictionary<string, (int Inflow, int Outflow)> flowByDept)
        {
            if (flowByDept == null || flowByDept.Count == 0) return null;

            var top = flowByDept
                .Select(kv => (Key: kv.Key, Inflow: kv.Value.Inflow, Outflow: kv.Value.Outflow, Net: kv.Value.Inflow - kv.Value.Outflow))
                .OrderByDescending(x => x.Net)
                .First();

            if (top.Net <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_BOTTLENECK",
                Severity = "warning",
                ReportRef = "flow",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = top.Key,
                    ["inflow"] = top.Inflow,
                    ["outflow"] = top.Outflow,
                    ["net"] = top.Net
                }
            };
        }

        // deltaPercent = null เมื่อ startWip = 0 (คำนวณ % การเติบโตไม่ได้) — ถือว่าไม่ trigger
        // เรียกได้หลายครั้งต่อแผนก (ต่างจาก rule อื่นที่ trigger ได้ครั้งเดียว) — Code ซ้ำกันได้ในรายการ problems
        public static Wip.Finding? EvaluateWipDeptGrowing(string deptKey, int startWip, int endWip, int thresholdPercent)
        {
            if (startWip <= 0) return null;

            var deltaPercent = Math.Round((decimal)(endWip - startWip) / startWip * 100, 2);
            if (deltaPercent < thresholdPercent) return null;

            var severity = deltaPercent >= thresholdPercent * 2 ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "WIP_DEPT_GROWING",
                Severity = severity,
                ReportRef = "trend",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["startWip"] = startWip,
                    ["endWip"] = endWip,
                    ["deltaPercent"] = deltaPercent,
                    ["thresholdPercent"] = thresholdPercent
                }
            };
        }

        // STAGE_OVER_STANDARD: median.total ของช่วง > standard — warning, เกิน >= 50% = critical
        // trigger ได้หลายครั้งต่อแผนก (เหมือน WIP_DEPT_GROWING)
        public static Wip.Finding? EvaluateStageOverStandard(string deptKey, double medianDays, decimal standardDays)
        {
            if (standardDays <= 0) return null;
            if (medianDays <= (double)standardDays) return null;

            var percent = Math.Round((decimal)(medianDays - (double)standardDays) / standardDays * 100, 1);
            var severity = percent >= ProductionInsightThresholds.StageOverStandardPercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "STAGE_OVER_STANDARD",
                Severity = severity,
                ReportRef = "leadTime",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["medianDays"] = medianDays,
                    ["standardDays"] = standardDays,
                    ["percent"] = percent
                }
            };
        }

        // STAGE_ABNORMAL_DWELL: แผนที่ยังอยู่ในแผนกนี้ตอนนี้เกิน multiplier×standard วัน (count มาจาก
        // ProductionStageLeadTimeEvaluator.BuildDepartmentStats.AbnormalCount — multiplier คงที่ 2 เท่า)
        public static Wip.Finding? EvaluateStageAbnormalDwell(string deptKey, int count, double thresholdDays)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "STAGE_ABNORMAL_DWELL",
                Severity = "warning",
                ReportRef = "abnormalDwell",
                Params = new Dictionary<string, object> { ["deptKey"] = deptKey, ["count"] = count, ["thresholdDays"] = thresholdDays }
            };
        }

        // STAGE_WAIT_DOMINANT: median รอ > median ทำงาน และรอ >= 50% ของเวลารวม — ต้องมี split sample
        // (exited visit ที่แยก wait/work ได้จริงจาก receive_date) อย่างน้อย MinSplitSamples ถึงจะเชื่อถือได้
        // (กันแจ้งเตือนจากข้อมูลน้อยเกินไปตอนเพิ่งเริ่มเก็บ receive_date — waitDays/workDays เป็น null ได้ถ้าไม่มี
        // sample เลย ซึ่งจะถูกกรองออกไปตั้งแต่เช็ค splitSampleCount ก่อนแล้ว)
        public static Wip.Finding? EvaluateStageWaitDominant(string deptKey, int splitSampleCount, double? waitDays, double? workDays)
        {
            if (splitSampleCount < ProductionInsightThresholds.MinSplitSamples) return null;
            if (!waitDays.HasValue || !workDays.HasValue) return null;

            var total = waitDays.Value + workDays.Value;
            if (total <= 0) return null;
            if (waitDays.Value <= workDays.Value) return null;

            var waitShare = Math.Round(waitDays.Value / total * 100, 1);
            if (waitShare < ProductionInsightThresholds.StageWaitDominantSharePercent) return null;

            return new Wip.Finding
            {
                Code = "STAGE_WAIT_DOMINANT",
                Severity = "warning",
                ReportRef = "leadTime",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["waitDays"] = waitDays.Value,
                    ["workDays"] = workDays.Value,
                    ["waitShare"] = waitShare
                }
            };
        }

        // FC_STAGE_LEADTIME_RISING: median total (เรียงตามเวลา) ของ bucket ที่ "ผ่านเกณฑ์" (count >=
        // MinSamplesPerBucket และมี medianTotal จริง ไม่ใช่ null) ล่าสุด N ตัว ต้องเรียงเพิ่มขึ้นต่อเนื่อง —
        // bucket ที่ข้อมูลไม่พอถูกข้ามไปเลย (ไม่นับเป็นหนึ่งใน N) กัน bucket ว่าง (medianTotal=null) หลอกว่า
        // "เพิ่มขึ้น" (false positive) — ถ้า qualifying bucket ไม่ถึง N ตัว ไม่ trigger
        public static Wip.Finding? EvaluateFcStageLeadtimeRising(string deptKey, IReadOnlyList<(int Count, double? MedianTotal)> series)
        {
            var n = ProductionInsightThresholds.StageLeadtimeRisingBucketCount;
            var minSamples = ProductionInsightThresholds.MinSamplesPerBucket;

            var qualifying = series
                .Where(s => s.Count >= minSamples && s.MedianTotal.HasValue)
                .Select(s => s.MedianTotal!.Value)
                .ToList();

            if (qualifying.Count < n) return null;

            var window = qualifying.Skip(qualifying.Count - n).ToList();
            for (var i = 1; i < window.Count; i++)
            {
                if (window[i] <= window[i - 1]) return null;
            }

            return new Wip.Finding
            {
                Code = "FC_STAGE_LEADTIME_RISING",
                Severity = "warning",
                ReportRef = "leadTime",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["fromDays"] = window[0],
                    ["toDays"] = window[window.Count - 1],
                    ["buckets"] = n
                }
            };
        }

        // ---- Delivery (ส่งงานตรงเวลา) ----

        // DLV_ONTIME_BELOW_TARGET: onTimePercent < target — warning, < target/2 = critical
        // bottleneckDeptKey แนบมาด้วยเผื่อ ACT_FIX_BOTTLENECK ใช้ (ไม่มีข้อมูลเป็น null)
        public static Wip.Finding? EvaluateDlvOnTimeBelowTarget(decimal? onTimePercent, decimal targetPercent, string? bottleneckDeptKey)
        {
            if (!onTimePercent.HasValue) return null;
            if (onTimePercent.Value >= targetPercent) return null;

            var severity = onTimePercent.Value < targetPercent / 2 ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "DLV_ONTIME_BELOW_TARGET",
                Severity = severity,
                ReportRef = "deliveryTrend",
                Params = new Dictionary<string, object>
                {
                    ["onTimePercent"] = onTimePercent.Value,
                    ["targetPercent"] = targetPercent,
                    ["bottleneckDept"] = bottleneckDeptKey ?? string.Empty
                }
            };
        }

        // DLV_LEAD_UNDERESTIMATED: planned median < 0.8 × actual median — เวลาที่ตกลงกับลูกค้าสั้นกว่าที่ทำได้จริงมาก
        public static Wip.Finding? EvaluateDlvLeadUnderestimated(double? plannedMedianDays, double? actualMedianDays, int suggestedLeadDays)
        {
            if (!plannedMedianDays.HasValue || !actualMedianDays.HasValue) return null;
            if (actualMedianDays.Value <= 0) return null;
            if (plannedMedianDays.Value >= actualMedianDays.Value * 0.8) return null;

            return new Wip.Finding
            {
                Code = "DLV_LEAD_UNDERESTIMATED",
                Severity = "warning",
                ReportRef = "deliveryTrend",
                Params = new Dictionary<string, object>
                {
                    ["planned"] = plannedMedianDays.Value,
                    ["actual"] = actualMedianDays.Value,
                    ["suggested"] = suggestedLeadDays
                }
            };
        }

        // DLV_OPEN_OVERDUE: แผน open ที่เกินกำหนดแล้วและยัง active (ไม่นับ stale — มี WIP_STALE ของตัวเองแล้ว)
        public static Wip.Finding? EvaluateDlvOpenOverdue(int activeOverdueCount, int openCount)
        {
            if (activeOverdueCount <= 0) return null;

            var percent = openCount > 0 ? Math.Round((decimal)activeOverdueCount / openCount * 100, 1) : 0m;
            var severity = percent >= ProductionInsightThresholds.WipOverduePercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "DLV_OPEN_OVERDUE",
                Severity = severity,
                ReportRef = "atRisk",
                Params = new Dictionary<string, object> { ["count"] = activeOverdueCount, ["openCount"] = openCount, ["percent"] = percent }
            };
        }

        // DLV_STUCK_AFTER_COSTCARD: เคยเข้าแผนกบัตรต้นทุน (95) แต่ไม่เคยถึง 100 (ไม่ใช่ 500/84/85)
        public static Wip.Finding? EvaluateDlvStuckAfterCostCard(int count)
        {
            if (count <= 0) return null;

            var severity = count >= ProductionInsightThresholds.DlvStuckAfterCostCardCountCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "DLV_STUCK_AFTER_COSTCARD",
                Severity = severity,
                ReportRef = "stuckCostCard",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        // FC_DLV_AT_RISK: จำนวนแผนที่ "เสี่ยงส่งช้า" (ยังไม่ถึงกำหนดแต่คาดว่าจะเสร็จช้ากว่ากำหนด) ภายในช่วงที่ขอ
        public static Wip.Finding? EvaluateFcDlvAtRisk(int count, int riskHorizonDays)
        {
            if (count <= 0) return null;

            var severity = count >= ProductionInsightThresholds.FcDueSoonAtRiskCountCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "FC_DLV_AT_RISK",
                Severity = severity,
                ReportRef = "atRisk",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = riskHorizonDays }
            };
        }

        // FC_DLV_ONTIME_DECLINING: onTimePercent ของ bucket ที่ "ผ่านเกณฑ์" (completedCount >= MinSamplesPerBucket)
        // ล่าสุด 3 ตัว ต้องเรียงลดลงต่อเนื่อง (ตรงข้ามกับ FC_STAGE_LEADTIME_RISING ที่เรียงเพิ่มขึ้น)
        public static Wip.Finding? EvaluateFcDlvOnTimeDeclining(
            IReadOnlyList<(int CompletedCount, decimal? OnTimePercent)> series, string? bottleneckDeptKey)
        {
            var n = ProductionInsightThresholds.StageLeadtimeRisingBucketCount;
            var minSamples = ProductionInsightThresholds.MinSamplesPerBucket;

            var qualifying = series
                .Where(s => s.CompletedCount >= minSamples && s.OnTimePercent.HasValue)
                .Select(s => s.OnTimePercent!.Value)
                .ToList();

            if (qualifying.Count < n) return null;

            var window = qualifying.Skip(qualifying.Count - n).ToList();
            for (var i = 1; i < window.Count; i++)
            {
                if (window[i] >= window[i - 1]) return null;
            }

            return new Wip.Finding
            {
                Code = "FC_DLV_ONTIME_DECLINING",
                Severity = "warning",
                ReportRef = "deliveryTrend",
                Params = new Dictionary<string, object>
                {
                    ["fromPercent"] = window[0],
                    ["toPercent"] = window[window.Count - 1],
                    ["buckets"] = n,
                    ["bottleneckDept"] = bottleneckDeptKey ?? string.Empty
                }
            };
        }

        // ---- Gold Loss (ทองและ Loss) ----
        // รอบแก้ไข 2026-10-01: เพิ่มมิติ metal ('GOLD'|'SILVER') ทุก finding, ปัดเศษใน params ทั้งหมด
        // (percent/gram 2 ตำแหน่ง, money 0 ตำแหน่ง), เพิ่ม tolerance ให้ rule เทียบ target, และรวม
        // GOLD_REPEAT_OFFENDER เป็น 1 finding ต่อ workerType×metal (ไม่ใช่ 1 ต่อช่างแบบเดิม)

        private static decimal RoundPercent(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundGram(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal RoundMoney(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);

        // GOLD_EXCESS_OVER_ALLOWANCE: ส่วนเกิน allowance รวม (กรัม) ต่อ workerType×metal — trigger ได้ครั้งเดียวต่อคู่
        public static Wip.Finding? EvaluateGoldExcessOverAllowance(int workerType, string metal, decimal excessGram, decimal excessMoney)
        {
            if (excessGram <= 0) return null;

            var severity = excessGram > ProductionInsightThresholds.GoldExcessOverAllowanceCriticalGram ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "GOLD_EXCESS_OVER_ALLOWANCE",
                Severity = severity,
                ReportRef = "goldOverSlips",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["excessGram"] = RoundGram(excessGram),
                    ["excessMoney"] = RoundMoney(excessMoney)
                }
            };
        }

        // GOLD_LOSS_ABOVE_TARGET (B): lossPercent (จริง) > targetPercent (เป้าหมายบริษัท) + tolerance
        public static Wip.Finding? EvaluateGoldLossAboveTarget(int workerType, string metal, decimal? lossPercent, decimal targetPercent)
        {
            if (!lossPercent.HasValue) return null;
            if (lossPercent.Value <= targetPercent + ProductionInsightThresholds.GoldTargetTolerancePercent) return null;

            var severity = lossPercent.Value >= targetPercent * ProductionInsightThresholds.GoldLossAboveTargetCriticalMultiplier
                ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "GOLD_LOSS_ABOVE_TARGET",
                Severity = severity,
                ReportRef = "goldKpi",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["lossPercent"] = RoundPercent(lossPercent.Value),
                    ["targetPercent"] = RoundPercent(targetPercent)
                }
            };
        }

        // GOLD_ALLOWANCE_ABOVE_TARGET (B vs A): allowedPercent (ที่ตั้งไว้บนใบ) > targetPercent (เป้าหมายบริษัท) +
        // tolerance — allowance ใจกว้างกว่านโยบายบริษัท (single-tier warning — เป็นเรื่อง policy ไม่ใช่ความเสียหายเฉียบพลัน)
        public static Wip.Finding? EvaluateGoldAllowanceAboveTarget(int workerType, string metal, decimal? allowedPercent, decimal targetPercent)
        {
            if (!allowedPercent.HasValue) return null;
            if (allowedPercent.Value <= targetPercent + ProductionInsightThresholds.GoldTargetTolerancePercent) return null;

            return new Wip.Finding
            {
                Code = "GOLD_ALLOWANCE_ABOVE_TARGET",
                Severity = "warning",
                ReportRef = "goldKpi",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["allowedPercent"] = RoundPercent(allowedPercent.Value),
                    ["targetPercent"] = RoundPercent(targetPercent)
                }
            };
        }

        // GOLD_MOST_WORKERS_OVER: >= 70% ของช่างประเภทนี้ (workerType×metal) เกิน allowance ของตัวเอง (A — "เกินเกณฑ์")
        public static Wip.Finding? EvaluateGoldMostWorkersOver(int workerType, string metal, int overCount, int workerCount)
        {
            if (workerCount <= 0 || overCount <= 0) return null;

            var percent = RoundPercent((decimal)overCount / workerCount * 100);
            if (percent < ProductionInsightThresholds.GoldMostWorkersOverPercent) return null;

            var severity = percent >= ProductionInsightThresholds.GoldMostWorkersOverPercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "GOLD_MOST_WORKERS_OVER",
                Severity = severity,
                ReportRef = "goldWorkers",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["overCount"] = overCount,
                    ["workerCount"] = workerCount,
                    ["percent"] = percent
                }
            };
        }

        // GOLD_REPEAT_OFFENDER: รวม 1 finding ต่อ workerType×metal — คัดช่างที่เกิน allowance ของตัวเอง (A —
        // "เกินเกณฑ์") ทุก bucket ที่ผ่านเกณฑ์ (qualifying) ติดต่อกัน >= 3 ครั้ง ใส่เป็น array ใน params.workers
        // (ไม่ trigger แยกต่อคนเหมือนรอบก่อน — UI ต้องวนแสดงเอง)
        public static Wip.Finding? EvaluateGoldRepeatOffender(
            int workerType, string metal, IReadOnlyList<(string WorkerCode, string? WorkerName, int OverBuckets, int QualifyingBuckets)> workers)
        {
            var minBuckets = ProductionInsightThresholds.GoldRepeatOffenderMinBuckets;
            var offenders = workers
                .Where(w => w.QualifyingBuckets >= minBuckets && w.OverBuckets == w.QualifyingBuckets)
                .ToList();

            if (offenders.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "GOLD_REPEAT_OFFENDER",
                Severity = "warning",
                ReportRef = "goldWorkers",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["workers"] = offenders.Select(o => new Dictionary<string, object>
                    {
                        ["workerCode"] = o.WorkerCode,
                        ["workerName"] = o.WorkerName ?? o.WorkerCode
                    }).ToList(),
                    ["count"] = offenders.Count,
                    ["buckets"] = minBuckets
                }
            };
        }

        // GOLD_SLIP_COVERAGE_LOW: % งาน (ที่เข้าเงื่อนไข) ที่ถูกตัดใบ gold loss ไปแล้วแล้ว < 80% (critical < 50%)
        public static Wip.Finding? EvaluateGoldSlipCoverageLow(int workerType, string metal, int coverageJobs, int coverageTotalJobs)
        {
            if (coverageTotalJobs <= 0) return null;

            var percent = RoundPercent((decimal)coverageJobs / coverageTotalJobs * 100);
            if (percent >= ProductionInsightThresholds.GoldSlipCoverageLowPercent) return null;

            var severity = percent < ProductionInsightThresholds.GoldSlipCoverageLowPercentCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "GOLD_SLIP_COVERAGE_LOW",
                Severity = severity,
                ReportRef = "goldUncovered",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["coveragePercent"] = percent,
                    ["coverageJobs"] = coverageJobs,
                    ["coverageTotalJobs"] = coverageTotalJobs
                }
            };
        }

        // FC_GOLD_EXCESS_PROJECTED: ยอดส่วนเกิน allowance เฉลี่ยต่อเดือน (กรัม/บาท) ถ้าแนวโน้มปัจจุบันดำเนินต่อ
        public static Wip.Finding? EvaluateFcGoldExcessProjected(int workerType, string metal, decimal excessGram, decimal excessMoney, double rangeDays)
        {
            if (excessGram <= 0 || rangeDays <= 0) return null;

            var avgMonthlyExcessGram = RoundGram(excessGram / (decimal)rangeDays * 30);
            var avgMonthlyExcessMoney = RoundMoney(excessMoney / (decimal)rangeDays * 30);

            return new Wip.Finding
            {
                Code = "FC_GOLD_EXCESS_PROJECTED",
                Severity = "warning",
                ReportRef = "goldTrend",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["avgMonthlyExcessGram"] = avgMonthlyExcessGram,
                    ["avgMonthlyExcessMoney"] = avgMonthlyExcessMoney
                }
            };
        }

        // FC_GOLD_LOSS_RISING: lossPercent ของ bucket ที่ผ่านเกณฑ์ (slipCount >= MinSamplesPerBucket) ล่าสุด 3 ตัว
        // ต้องเรียงเพิ่มขึ้นต่อเนื่อง (รูปแบบเดียวกับ FC_STAGE_LEADTIME_RISING)
        public static Wip.Finding? EvaluateFcGoldLossRising(int workerType, string metal, IReadOnlyList<(int SlipCount, decimal? LossPercent)> series)
        {
            var n = ProductionInsightThresholds.StageLeadtimeRisingBucketCount;
            var minSamples = ProductionInsightThresholds.MinSamplesPerBucket;

            var qualifying = series
                .Where(s => s.SlipCount >= minSamples && s.LossPercent.HasValue)
                .Select(s => s.LossPercent!.Value)
                .ToList();

            if (qualifying.Count < n) return null;

            var window = qualifying.Skip(qualifying.Count - n).ToList();
            for (var i = 1; i < window.Count; i++)
            {
                if (window[i] <= window[i - 1]) return null;
            }

            return new Wip.Finding
            {
                Code = "FC_GOLD_LOSS_RISING",
                Severity = "warning",
                ReportRef = "goldTrend",
                Params = new Dictionary<string, object>
                {
                    ["workerType"] = workerType,
                    ["metal"] = metal,
                    ["fromPercent"] = RoundPercent(window[0]),
                    ["toPercent"] = RoundPercent(window[window.Count - 1]),
                    ["buckets"] = n
                }
            };
        }

        // 'critical' | 'warning' | 'ok' (ไม่มี finding เลย = ok)
        public static string WorstSeverity(IEnumerable<Wip.Finding> findings)
        {
            var list = findings.ToList();
            if (list.Any(f => f.Severity == "critical")) return "critical";
            if (list.Any(f => f.Severity == "warning")) return "warning";
            if (list.Any()) return "info";
            return "ok";
        }

        private static readonly (string Code, string OwnerRole, string[] RelatedCodes)[] ActionDefinitions =
        {
            ("ACT_CLOSE_STALE", "deptHead", new[] { "WIP_STALE", "FC_BECOMING_STALE" }),
            ("ACT_PRIORITIZE_DUE", "planner", new[] { "WIP_OVERDUE", "FC_DUE_SOON_AT_RISK" }),
            ("ACT_STAGE_SLA", "productionManager", new[] { "WIP_DEPT_STALE_TOP", "FC_BOTTLENECK", "WIP_DEPT_GROWING", "STAGE_OVER_STANDARD", "FC_STAGE_LEADTIME_RISING" }),
            ("ACT_CLOSE_MELTED", "goldControl", new[] { "WIP_MELTED_OPEN" }),
            ("ACT_REDUCE_WAIT", "productionManager", new[] { "STAGE_WAIT_DOMINANT" }),
            ("ACT_REVIEW_ABNORMAL", "deptHead", new[] { "STAGE_ABNORMAL_DWELL" }),
            ("ACT_SET_REALISTIC_DUE", "planner", new[] { "DLV_LEAD_UNDERESTIMATED" }),
            ("ACT_EXPEDITE_AT_RISK", "planner", new[] { "FC_DLV_AT_RISK", "DLV_OPEN_OVERDUE" }),
            ("ACT_CLOSE_COSTCARD", "goldControl", new[] { "DLV_STUCK_AFTER_COSTCARD" }),
            ("ACT_FIX_BOTTLENECK", "productionManager", new[] { "DLV_ONTIME_BELOW_TARGET", "FC_DLV_ONTIME_DECLINING" }),
            ("ACT_COMPLETE_SLIPS", "deptHead", new[] { "GOLD_SLIP_COVERAGE_LOW" }),
            ("ACT_TALK_WORKER", "deptHead", new[] { "GOLD_REPEAT_OFFENDER" }),
            ("ACT_REVIEW_ALLOWANCE", "productionManager", new[] { "GOLD_ALLOWANCE_ABOVE_TARGET", "GOLD_LOSS_ABOVE_TARGET" }),
            ("ACT_CHECK_WEIGHING", "deptHead", new[] { "GOLD_EXCESS_OVER_ALLOWANCE", "GOLD_MOST_WORKERS_OVER" })
        };

        // priority: เรียงตาม severity ที่แย่ที่สุดของ finding ที่เกี่ยวข้องก่อน (critical > warning) แล้วตามลำดับ
        // ที่ประกาศไว้ใน ActionDefinitions — ข้าม action ที่ไม่มี related code ไหนถูก trigger เลย
        // ใช้ ILookup ไม่ใช่ Dictionary เพราะ WIP_DEPT_GROWING trigger ได้หลายครั้ง (Code ซ้ำกันได้ในรายการ)
        public static List<Wip.ActionItem> BuildActions(IReadOnlyList<Wip.Finding> problems, IReadOnlyList<Wip.Finding> forecasts)
        {
            var findingsByCode = problems.Concat(forecasts).ToLookup(f => f.Code);
            var built = new List<(Wip.ActionItem Action, int SeverityRank, int DeclaredIndex)>();

            for (var i = 0; i < ActionDefinitions.Length; i++)
            {
                var def = ActionDefinitions[i];
                var relatedFindings = def.RelatedCodes
                    .SelectMany(code => findingsByCode[code])
                    .ToList();

                if (relatedFindings.Count == 0) continue;

                var severityRank = relatedFindings.Min(f => SeverityRank(f.Severity));

                var action = new Wip.ActionItem
                {
                    Code = def.Code,
                    OwnerRole = def.OwnerRole,
                    RelatedCodes = def.RelatedCodes.ToList(),
                    Params = BuildActionParams(def.Code, findingsByCode)
                };

                built.Add((action, severityRank, i));
            }

            var ordered = built.OrderBy(x => x.SeverityRank).ThenBy(x => x.DeclaredIndex).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Action.Priority = i + 1;
            }

            return ordered.Select(x => x.Action).ToList();
        }

        private static int SeverityRank(string severity) => severity switch
        {
            "critical" => 0,
            "warning" => 1,
            _ => 2
        };

        private static Dictionary<string, object> BuildActionParams(string actionCode, ILookup<string, Wip.Finding> findings)
        {
            switch (actionCode)
            {
                case "ACT_CLOSE_STALE":
                {
                    var count = GetParam(findings, "WIP_STALE", "count") ?? GetParam(findings, "FC_BECOMING_STALE", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_PRIORITIZE_DUE":
                {
                    var overdue = GetParam(findings, "WIP_OVERDUE", "count") ?? 0;
                    var dueSoon = GetParam(findings, "FC_DUE_SOON_AT_RISK", "count") ?? 0;
                    return new Dictionary<string, object> { ["overdue"] = overdue, ["dueSoon"] = dueSoon };
                }
                case "ACT_STAGE_SLA":
                {
                    var deptKey = GetParam(findings, "WIP_DEPT_STALE_TOP", "deptKey")
                        ?? GetParam(findings, "FC_BOTTLENECK", "deptKey")
                        ?? GetParam(findings, "WIP_DEPT_GROWING", "deptKey")
                        ?? GetParam(findings, "STAGE_OVER_STANDARD", "deptKey")
                        ?? GetParam(findings, "FC_STAGE_LEADTIME_RISING", "deptKey");
                    return new Dictionary<string, object> { ["deptKey"] = deptKey ?? string.Empty };
                }
                case "ACT_CLOSE_MELTED":
                {
                    var count = GetParam(findings, "WIP_MELTED_OPEN", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_REDUCE_WAIT":
                {
                    var deptKey = GetParam(findings, "STAGE_WAIT_DOMINANT", "deptKey");
                    return new Dictionary<string, object> { ["deptKey"] = deptKey ?? string.Empty };
                }
                case "ACT_REVIEW_ABNORMAL":
                {
                    var count = GetParam(findings, "STAGE_ABNORMAL_DWELL", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_SET_REALISTIC_DUE":
                {
                    var suggested = GetParam(findings, "DLV_LEAD_UNDERESTIMATED", "suggested") ?? 0;
                    return new Dictionary<string, object> { ["suggestedLeadDays"] = suggested };
                }
                case "ACT_EXPEDITE_AT_RISK":
                {
                    var atRisk = GetParam(findings, "FC_DLV_AT_RISK", "count") ?? 0;
                    var overdue = GetParam(findings, "DLV_OPEN_OVERDUE", "count") ?? 0;
                    return new Dictionary<string, object> { ["atRisk"] = atRisk, ["overdue"] = overdue };
                }
                case "ACT_CLOSE_COSTCARD":
                {
                    var count = GetParam(findings, "DLV_STUCK_AFTER_COSTCARD", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_FIX_BOTTLENECK":
                {
                    var deptKey = GetParam(findings, "DLV_ONTIME_BELOW_TARGET", "bottleneckDept")
                        ?? GetParam(findings, "FC_DLV_ONTIME_DECLINING", "bottleneckDept");
                    return new Dictionary<string, object> { ["deptKey"] = deptKey ?? string.Empty };
                }
                case "ACT_COMPLETE_SLIPS":
                {
                    var workerType = GetParam(findings, "GOLD_SLIP_COVERAGE_LOW", "workerType") ?? 0;
                    var jobsObj = GetParam(findings, "GOLD_SLIP_COVERAGE_LOW", "coverageJobs");
                    var totalObj = GetParam(findings, "GOLD_SLIP_COVERAGE_LOW", "coverageTotalJobs");
                    var uncovered = (totalObj is int t ? t : 0) - (jobsObj is int j ? j : 0);
                    var metalCover = GetParam(findings, "GOLD_SLIP_COVERAGE_LOW", "metal") ?? string.Empty;
                    return new Dictionary<string, object> { ["workerType"] = workerType, ["metal"] = metalCover, ["uncoveredCount"] = uncovered };
                }
                case "ACT_TALK_WORKER":
                {
                    var workerType = GetParam(findings, "GOLD_REPEAT_OFFENDER", "workerType") ?? 0;
                    var metalTalk = GetParam(findings, "GOLD_REPEAT_OFFENDER", "metal") ?? string.Empty;
                    var workersObj = GetParam(findings, "GOLD_REPEAT_OFFENDER", "workers");
                    var workersList = workersObj as List<Dictionary<string, object>> ?? new List<Dictionary<string, object>>();
                    var topWorkers = workersList.Take(5).ToList();
                    return new Dictionary<string, object>
                    {
                        ["workerType"] = workerType,
                        ["metal"] = metalTalk,
                        ["workers"] = topWorkers,
                        ["count"] = workersList.Count
                    };
                }
                case "ACT_REVIEW_ALLOWANCE":
                {
                    var workerType = GetParam(findings, "GOLD_ALLOWANCE_ABOVE_TARGET", "workerType")
                        ?? GetParam(findings, "GOLD_LOSS_ABOVE_TARGET", "workerType") ?? 0;
                    var metalReview = GetParam(findings, "GOLD_ALLOWANCE_ABOVE_TARGET", "metal")
                        ?? GetParam(findings, "GOLD_LOSS_ABOVE_TARGET", "metal") ?? string.Empty;
                    var allowedPercent = GetParam(findings, "GOLD_ALLOWANCE_ABOVE_TARGET", "allowedPercent") ?? 0m;
                    var lossPercent = GetParam(findings, "GOLD_LOSS_ABOVE_TARGET", "lossPercent") ?? 0m;
                    var targetPercent = GetParam(findings, "GOLD_ALLOWANCE_ABOVE_TARGET", "targetPercent")
                        ?? GetParam(findings, "GOLD_LOSS_ABOVE_TARGET", "targetPercent") ?? 0m;
                    // หมายเหตุ: ค่าทั้งหมดนี้ถูกเติมทับด้วย kpi row จริงอีกรอบใน ProductionInsightService.EnrichGoldActionParams
                    // (กันกรณี trigger จาก finding เดียว ทำให้ field ที่มาจากอีก finding เป็น 0 หลอกๆ)
                    return new Dictionary<string, object> { ["workerType"] = workerType, ["metal"] = metalReview, ["lossPercent"] = lossPercent, ["allowedPercent"] = allowedPercent, ["targetPercent"] = targetPercent };
                }
                case "ACT_CHECK_WEIGHING":
                {
                    var workerType = GetParam(findings, "GOLD_EXCESS_OVER_ALLOWANCE", "workerType")
                        ?? GetParam(findings, "GOLD_MOST_WORKERS_OVER", "workerType") ?? 0;
                    var metalCheck = GetParam(findings, "GOLD_EXCESS_OVER_ALLOWANCE", "metal")
                        ?? GetParam(findings, "GOLD_MOST_WORKERS_OVER", "metal") ?? string.Empty;
                    var excessGram = GetParam(findings, "GOLD_EXCESS_OVER_ALLOWANCE", "excessGram") ?? 0m;
                    return new Dictionary<string, object> { ["workerType"] = workerType, ["metal"] = metalCheck, ["excessGram"] = excessGram };
                }
                default:
                    return new Dictionary<string, object>();
            }
        }

        // เอา finding แรกของ code นั้น (เรียงตามลำดับแผนกที่ declare ไว้ — เพียงพอสำหรับ param เดี่ยวแบบนี้)
        private static object? GetParam(ILookup<string, Wip.Finding> findings, string code, string paramKey)
        {
            var finding = findings[code].FirstOrDefault();
            return finding != null && finding.Params.TryGetValue(paramKey, out var value) ? value : null;
        }

        private static decimal PercentOf(int part, int total)
        {
            return total > 0 ? Math.Round((decimal)part / total * 100, 1) : 0m;
        }
    }
}
