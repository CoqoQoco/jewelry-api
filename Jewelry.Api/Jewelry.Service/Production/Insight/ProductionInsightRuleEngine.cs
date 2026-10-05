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

        // ---- Capacity (กำลังการผลิต) ----

        // CAP_BACKLOG_MONTHS: activeWip ÷ outputPerMonth (เดือน) > 2 = warning, > 4 = critical
        public static Wip.Finding? EvaluateCapBacklogMonths(decimal? backlogMonths, int activeWip, decimal outputPerMonth)
        {
            if (!backlogMonths.HasValue) return null;
            if (backlogMonths.Value <= ProductionInsightThresholds.CapBacklogMonthsWarn) return null;

            var severity = backlogMonths.Value > ProductionInsightThresholds.CapBacklogMonthsCritical ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "CAP_BACKLOG_MONTHS",
                Severity = severity,
                ReportRef = "capKpi",
                Params = new Dictionary<string, object>
                {
                    ["backlogMonths"] = Math.Round(backlogMonths.Value, 1),
                    ["activeWip"] = activeWip,
                    ["outputPerMonth"] = Math.Round(outputPerMonth, 1)
                }
            };
        }

        // CAP_QUEUE_BOTTLENECK: 1 finding รวม top 2 แผนกคิวยาวสุด (queueDays = activeWip ÷ per-day — ดู
        // BuildCapacity) — ไม่แยก trigger ต่อแผนกเหมือน WIP_DEPT_GROWING
        public static Wip.Finding? EvaluateCapQueueBottleneck(IReadOnlyList<(string DeptKey, double QueueDays, int WaitingNow)> topDepts)
        {
            if (topDepts.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "CAP_QUEUE_BOTTLENECK",
                Severity = "warning",
                ReportRef = "capDepartments",
                Params = new Dictionary<string, object>
                {
                    ["depts"] = topDepts.Select(d => new Dictionary<string, object>
                    {
                        ["deptKey"] = d.DeptKey,
                        ["queueDays"] = Math.Round(d.QueueDays, 1),
                        ["waitingNow"] = d.WaitingNow
                    }).ToList()
                }
            };
        }

        // CAP_INFLOW_OVER_OUTPUT: จำนวนเดือนในช่วงที่ inflow > output >= 2 เดือน
        public static Wip.Finding? EvaluateCapInflowOverOutput(int overloadMonths, int monthsInRange, string? peakMonth, int peakInflow, decimal outputPerMonth)
        {
            if (overloadMonths < ProductionInsightThresholds.CapOverloadMonthsThreshold) return null;

            return new Wip.Finding
            {
                Code = "CAP_INFLOW_OVER_OUTPUT",
                Severity = "warning",
                ReportRef = "capTrend",
                Params = new Dictionary<string, object>
                {
                    ["overloadMonths"] = overloadMonths,
                    ["monthsInRange"] = monthsInRange,
                    ["peakMonth"] = peakMonth ?? string.Empty,
                    ["peakInflow"] = peakInflow,
                    ["outputPerMonth"] = Math.Round(outputPerMonth, 1)
                }
            };
        }

        // CAP_COSTCARD_SLOW: median 95→100 > 7 วัน หรือค้างเกิน 30 วันมากกว่า 50 แผน (OR เงื่อนไข)
        public static Wip.Finding? EvaluateCapCostCardSlow(double? medianDays, double? p90Days, int pendingNow, int pendingActive, int pendingOver30d)
        {
            var triggered = (medianDays.HasValue && medianDays.Value > ProductionInsightThresholds.CapCostCardSlowMedianDaysThreshold)
                || pendingOver30d > ProductionInsightThresholds.CapCostCardSlowPendingOver30dThreshold;
            if (!triggered) return null;

            return new Wip.Finding
            {
                Code = "CAP_COSTCARD_SLOW",
                Severity = "warning",
                ReportRef = "capCostCard",
                Params = new Dictionary<string, object>
                {
                    ["medianDays"] = medianDays.HasValue ? Math.Round(medianDays.Value, 1) : 0,
                    ["p90Days"] = p90Days.HasValue ? Math.Round(p90Days.Value, 1) : 0,
                    ["pendingNow"] = pendingNow,
                    ["pendingActive"] = pendingActive,
                    ["pendingOver30d"] = pendingOver30d
                }
            };
        }

        // FC_BACKLOG_PROJECTED: WIP คาดการณ์อีก 3 เดือน (activeWip + 3×netPerMonth) — trigger เมื่อ net เป็นบวก
        // (WIP กำลังโตตามเทรนด์ปัจจุบัน) เท่านั้น — net ติดลบ/0 = ไม่ต้องเตือนล่วงหน้า
        public static Wip.Finding? EvaluateFcBacklogProjected(int activeWip, decimal netPerMonth)
        {
            if (netPerMonth <= 0) return null;

            var months = ProductionInsightThresholds.FcBacklogProjectedMonths;
            var projectedWip = activeWip + months * netPerMonth;

            return new Wip.Finding
            {
                Code = "FC_BACKLOG_PROJECTED",
                Severity = "warning",
                ReportRef = "capTrend",
                Params = new Dictionary<string, object>
                {
                    ["projectedWip"] = Math.Round(projectedWip, 1),
                    ["months"] = months,
                    ["netPerMonth"] = Math.Round(netPerMonth, 1)
                }
            };
        }

        // FC_PEAK_RISK: มีเดือนในอดีตที่ inflow > 1.5×output — extraQueueDays คำนวณมาแล้วจาก service (ส่วนเกินของ
        // peak ÷ per-day ของแผนกคอขวดอันดับ 1)
        public static Wip.Finding? EvaluateFcPeakRisk(string? peakMonth, int peakInflow, decimal outputPerMonth, double extraQueueDays)
        {
            if (string.IsNullOrEmpty(peakMonth)) return null;
            if (peakInflow <= (double)outputPerMonth * (double)ProductionInsightThresholds.FcPeakRiskMultiplier) return null;

            return new Wip.Finding
            {
                Code = "FC_PEAK_RISK",
                Severity = "warning",
                ReportRef = "capTrend",
                Params = new Dictionary<string, object>
                {
                    ["peakMonth"] = peakMonth,
                    ["peakInflow"] = peakInflow,
                    ["extraQueueDays"] = Math.Round(extraQueueDays, 1)
                }
            };
        }

        // ---- Gold Loss by Stage (Loss ตามใบงานรายแผนก จ่าย-รับ) ----

        // GOLD_STAGE_ABOVE_TARGET: diffPercent (รายแผนก) > targetPercent (STAGE) + tolerance — trigger ได้หลาย
        // ครั้ง (คนละแผนก) เหมือน WIP_DEPT_GROWING — trim/gemSort ไม่มี targetPercent (null) จึงไม่ trigger เอง
        public static Wip.Finding? EvaluateGoldStageAboveTarget(string deptKey, string metal, decimal? diffPercent, decimal? targetPercent)
        {
            if (!diffPercent.HasValue || !targetPercent.HasValue) return null;
            if (diffPercent.Value <= targetPercent.Value + ProductionInsightThresholds.GoldStageTargetTolerancePercent) return null;

            return new Wip.Finding
            {
                Code = "GOLD_STAGE_ABOVE_TARGET",
                Severity = "warning",
                ReportRef = "goldStage",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["metal"] = metal,
                    ["diffPercent"] = Math.Round(diffPercent.Value, 2),
                    ["targetPercent"] = Math.Round(targetPercent.Value, 2)
                }
            };
        }

        // GOLD_STAGE_PENDING_RETURN: 1 finding รวมทุกแผนก — งานที่ยังไม่คืนค้างเกิน 14 วัน
        public static Wip.Finding? EvaluateGoldStagePendingReturn(string metal, int count, decimal gram, string? topDeptKey)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "GOLD_STAGE_PENDING_RETURN",
                Severity = "warning",
                ReportRef = "goldStagePending",
                Params = new Dictionary<string, object>
                {
                    ["metal"] = metal,
                    ["count"] = count,
                    ["gram"] = Math.Round(gram, 2),
                    ["topDeptKey"] = topDeptKey ?? string.Empty
                }
            };
        }

        // GOLD_STAGE_QUEUED: ของที่ "รอจ่าย" (placeholder/ไม่มีช่างถือครอง) สะสมอยู่ — info-level (ไม่ใช่ปัญหาต้อง
        // แก้ด่วนเหมือน PENDING_RETURN ที่มีคนถือของจริงแต่ยังไม่คืน — นี่คือของที่ "ยังไม่ออกให้ใครเลย")
        public static Wip.Finding? EvaluateGoldStageQueued(string metal, int count, decimal gram, string? topDeptKey)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "GOLD_STAGE_QUEUED",
                Severity = "info",
                ReportRef = "goldStagePending",
                Params = new Dictionary<string, object>
                {
                    ["metal"] = metal,
                    ["count"] = count,
                    ["gram"] = Math.Round(gram, 2),
                    ["topDeptKey"] = topDeptKey ?? string.Empty
                }
            };
        }

        // GOLD_STAGE_OUTLIER_JOBS: จำนวนงานผิดปกติรวมทุกแผนก (ไม่รวม trim/gemSort) >= 5
        public static Wip.Finding? EvaluateGoldStageOutlierJobs(string metal, int count)
        {
            if (count < ProductionInsightThresholds.GoldStageOutlierJobsCountThreshold) return null;

            return new Wip.Finding
            {
                Code = "GOLD_STAGE_OUTLIER_JOBS",
                Severity = "warning",
                ReportRef = "goldStageOutliers",
                Params = new Dictionary<string, object> { ["metal"] = metal, ["count"] = count }
            };
        }

        // FC_GOLD_STAGE_RISING: diffPercent ของ bucket ที่มีค่า (ไม่ null) ล่าสุด 3 ตัว ต้องเรียงเพิ่มขึ้นต่อเนื่อง
        // — series ของหน้านี้ไม่มี "count" ต่อ bucket ให้กรองแบบ MinSamplesPerBucket เหมือนที่อื่น จึงใช้
        // diffPercent.HasValue เป็นตัวกรอง "qualifying" แทน (bucket ที่ไม่มีแถวคืนแล้วเลยถูกข้าม)
        public static Wip.Finding? EvaluateFcGoldStageRising(string deptKey, string metal, IReadOnlyList<decimal?> seriesDiffPercent)
        {
            var n = ProductionInsightThresholds.StageLeadtimeRisingBucketCount;
            var qualifying = seriesDiffPercent.Where(p => p.HasValue).Select(p => p!.Value).ToList();
            if (qualifying.Count < n) return null;

            var window = qualifying.Skip(qualifying.Count - n).ToList();
            for (var i = 1; i < window.Count; i++)
            {
                if (window[i] <= window[i - 1]) return null;
            }

            return new Wip.Finding
            {
                Code = "FC_GOLD_STAGE_RISING",
                Severity = "warning",
                ReportRef = "goldStage",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["metal"] = metal,
                    ["fromPercent"] = Math.Round(window[0], 2),
                    ["toPercent"] = Math.Round(window[window.Count - 1], 2)
                }
            };
        }

        // ---- ช่างและค่าแรง (Workers) ----

        // WRK_CONCENTRATION: trigger ได้หลายครั้ง (คนละแผนก) เหมือน WIP_DEPT_GROWING — top2SharePercent เป็นสเกล 0-100
        public static Wip.Finding? EvaluateWrkConcentration(
            string deptKey, int activeWorkerCount, decimal top2SharePercent, IReadOnlyList<(string Code, string Name)> topWorkers)
        {
            if (activeWorkerCount < ProductionInsightThresholds.WrkConcentrationMinWorkers) return null;
            if (top2SharePercent < ProductionInsightThresholds.WrkConcentrationTop2SharePercent) return null;

            return new Wip.Finding
            {
                Code = "WRK_CONCENTRATION",
                Severity = "warning",
                ReportRef = "wrkTable",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["top2Share"] = Math.Round(top2SharePercent, 2),
                    ["workers"] = topWorkers.Select(w => new Dictionary<string, object> { ["code"] = w.Code, ["name"] = w.Name }).ToList()
                }
            };
        }

        // WRK_RATE_OUTLIER: 1 finding รวม — count ทั้งหมด + top 3 (เรียงตามอัตราส่วน wagePerJob/medianPerJob มากสุด)
        public static Wip.Finding? EvaluateWrkRateOutlier(IReadOnlyList<(string Code, string Name, decimal WagePerJob, decimal MedianPerJob)> outliers)
        {
            if (outliers.Count == 0) return null;

            var top3 = outliers
                .OrderByDescending(o => o.MedianPerJob > 0 ? o.WagePerJob / o.MedianPerJob : o.WagePerJob)
                .Take(3)
                .ToList();

            return new Wip.Finding
            {
                Code = "WRK_RATE_OUTLIER",
                Severity = "warning",
                ReportRef = "wrkTable",
                Params = new Dictionary<string, object>
                {
                    ["count"] = outliers.Count,
                    ["workers"] = top3.Select(o => new Dictionary<string, object>
                    {
                        ["code"] = o.Code,
                        ["name"] = o.Name,
                        ["wagePerJob"] = Math.Round(o.WagePerJob, 2),
                        ["medianPerJob"] = Math.Round(o.MedianPerJob, 2)
                    }).ToList()
                }
            };
        }

        // WRK_WAGE_PER_PLAN_RISING: wagePerPlan ของ bucket เต็มเดือน (full-month) ล่าสุด 3 ตัว ต้องเรียงเพิ่มขึ้นต่อเนื่อง
        public static Wip.Finding? EvaluateWrkWagePerPlanRising(IReadOnlyList<(bool IsFullMonth, decimal? WagePerPlan)> series)
        {
            var n = ProductionInsightThresholds.StageLeadtimeRisingBucketCount;
            var qualifying = series.Where(s => s.IsFullMonth && s.WagePerPlan.HasValue).Select(s => s.WagePerPlan!.Value).ToList();
            if (qualifying.Count < n) return null;

            var window = qualifying.Skip(qualifying.Count - n).ToList();
            for (var i = 1; i < window.Count; i++)
            {
                if (window[i] <= window[i - 1]) return null;
            }

            return new Wip.Finding
            {
                Code = "WRK_WAGE_PER_PLAN_RISING",
                Severity = "warning",
                ReportRef = "wrkTrend",
                Params = new Dictionary<string, object>
                {
                    ["fromValue"] = Math.Round(window[0], 2),
                    ["toValue"] = Math.Round(window[window.Count - 1], 2)
                }
            };
        }

        // WRK_UNPAID_JOBS: มีงานที่ยังไม่บันทึกค่าแรงอย่างน้อย 1 ชิ้น
        public static Wip.Finding? EvaluateWrkUnpaidJobs(int count)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "WRK_UNPAID_JOBS",
                Severity = "warning",
                ReportRef = "wrkUnpaid",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        // WRK_GOLD_REPEAT: reuse เกณฑ์เดียวกับ GOLD_REPEAT_OFFENDER (เกิน allowance ตัวเองทุก bucket ที่ผ่านเกณฑ์
        // ติดต่อกัน >= 3 ครั้ง) metal=GOLD เท่านั้น — คัดจาก tang(50)+setting(80) รวมกัน
        public static Wip.Finding? EvaluateWrkGoldRepeat(IReadOnlyList<(string Code, string Name)> workers)
        {
            if (workers.Count == 0) return null;

            return new Wip.Finding
            {
                Code = "WRK_GOLD_REPEAT",
                Severity = "warning",
                ReportRef = "wrkTable",
                Params = new Dictionary<string, object>
                {
                    ["workers"] = workers.Select(w => new Dictionary<string, object> { ["code"] = w.Code, ["name"] = w.Name }).ToList(),
                    ["count"] = workers.Count
                }
            };
        }

        // FC_WAGES_NEXT_MONTH: เฉลี่ย 3 เดือนเต็มล่าสุด + แนวโน้มเชิงเส้น (simple linear regression บน 3 จุด) —
        // severity info (เป็นตัวเลขคาดการณ์เฉยๆ ไม่ใช่ปัญหาที่ต้องแก้)
        public static Wip.Finding? EvaluateFcWagesNextMonth(IReadOnlyList<decimal> lastFullMonthsWages)
        {
            var n = lastFullMonthsWages.Count;
            if (n < 3) return null;

            var avgWages = Math.Round(lastFullMonthsWages.Average(), 0);

            var xs = Enumerable.Range(0, n).Select(i => (decimal)i).ToList();
            var xMean = xs.Average();
            var yMean = lastFullMonthsWages.Average();
            var numerator = 0m;
            var denominator = 0m;
            for (var i = 0; i < n; i++)
            {
                numerator += (xs[i] - xMean) * (lastFullMonthsWages[i] - yMean);
                denominator += (xs[i] - xMean) * (xs[i] - xMean);
            }
            var slope = denominator != 0 ? numerator / denominator : 0m;
            var projectedWages = Math.Round(lastFullMonthsWages[n - 1] + slope, 0);

            return new Wip.Finding
            {
                Code = "FC_WAGES_NEXT_MONTH",
                Severity = "info",
                ReportRef = "wrkTrend",
                Params = new Dictionary<string, object> { ["projectedWages"] = projectedWages, ["avgWages"] = avgWages }
            };
        }

        // FC_KEY_PERSON_RISK: สำหรับแผนกที่กระจุกตัวที่สุด — ถ้าช่างอันดับ 1 หายไป exits/วัน ลดลงตามสัดส่วนงานของเขา
        // → คิวยาวขึ้น (activeWip เดิม ÷ exits/วันที่ลดลง) — trigger เฉพาะกรณีที่คิวยาวขึ้นจริง (queueDaysWithout > queueDaysNow)
        public static Wip.Finding? EvaluateFcKeyPersonRisk(string deptKey, string workerName, double? queueDaysNow, double? queueDaysWithout)
        {
            if (!queueDaysNow.HasValue || !queueDaysWithout.HasValue) return null;
            if (queueDaysWithout.Value <= queueDaysNow.Value) return null;

            return new Wip.Finding
            {
                Code = "FC_KEY_PERSON_RISK",
                Severity = "warning",
                ReportRef = "wrkKpi",
                Params = new Dictionary<string, object>
                {
                    ["deptKey"] = deptKey,
                    ["workerName"] = workerName,
                    ["queueDaysNow"] = Math.Round(queueDaysNow.Value, 1),
                    ["queueDaysWithout"] = Math.Round(queueDaysWithout.Value, 1)
                }
            };
        }

        // ---- วัตถุดิบที่กระทบการผลิต (Materials — gems only) ----

        // MAT_GEM_WAITING: แผนอยู่แผนกตัดพลอย ไม่ stale ยังไม่เบิกพลอยจริงเลย (count/medianDays มาจาก kpi ตรงๆ)
        public static Wip.Finding? EvaluateMatGemWaiting(int count, double? medianDays)
        {
            if (count <= 0) return null;

            var severity = medianDays.HasValue && medianDays.Value > ProductionInsightThresholds.MatGemWaitingCriticalDays ? "critical" : "warning";

            return new Wip.Finding
            {
                Code = "MAT_GEM_WAITING",
                Severity = severity,
                ReportRef = "matWaiting",
                Params = new Dictionary<string, object> { ["count"] = count, ["medianDays"] = medianDays.HasValue ? Math.Round(medianDays.Value, 1) : 0 }
            };
        }

        // MAT_READY_NOT_ISSUED: แผนที่อยู่แผนกตัดพลอยอยู่แล้ว วัตถุดิบพลอยพร้อมครบ (ready) แต่ยังไม่มีการเบิกจริง —
        // ปัญหาขั้นตอน/คิวงาน ไม่ใช่ปัญหาของขาด
        public static Wip.Finding? EvaluateMatReadyNotIssued(int count)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_READY_NOT_ISSUED",
                Severity = "warning",
                ReportRef = "matKpi",
                Params = new Dictionary<string, object> { ["count"] = count }
            };
        }

        // MAT_GEM_SHORT: แผนที่มีบรรทัดวัตถุดิบพลอยจับคู่ของได้แต่ไม่พอ (short) อย่างน้อย 1 บรรทัด
        public static Wip.Finding? EvaluateMatGemShort(int plans, int lines)
        {
            if (plans <= 0) return null;

            return new Wip.Finding
            {
                Code = "MAT_GEM_SHORT",
                Severity = "warning",
                ReportRef = "matDemand",
                Params = new Dictionary<string, object> { ["plans"] = plans, ["lines"] = lines }
            };
        }

        // MAT_SPEC_UNMATCHED: บรรทัดที่หา stock จับคู่ไม่ได้เลย (ข้อมูล master ไม่ครบ/ไม่ตรง) — info level
        // (เป็นปัญหาคุณภาพข้อมูล ไม่ใช่ของขาดจริงเสมอไป)
        public static Wip.Finding? EvaluateMatSpecUnmatched(int lines, int totalLines)
        {
            if (lines <= 0) return null;

            var percent = totalLines > 0 ? Math.Round((decimal)lines / totalLines * 100, 1) : 0m;

            return new Wip.Finding
            {
                Code = "MAT_SPEC_UNMATCHED",
                Severity = "info",
                ReportRef = "matDemand",
                Params = new Dictionary<string, object> { ["lines"] = lines, ["percent"] = percent }
            };
        }

        // FC_GEM_SHORT_UPCOMING: แผนที่ยังไม่ถึงแผนกตัดพลอย (สถานะ 10,49,50,59,60) ต้องการ spec ที่ "short" อยู่แล้ว
        // ตอนนี้ — เตือนล่วงหน้าก่อนแผนเหล่านี้มาถึงแผนกตัดพลอยจริง
        public static Wip.Finding? EvaluateFcGemShortUpcoming(int plans, int lines)
        {
            if (plans <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_GEM_SHORT_UPCOMING",
                Severity = "warning",
                ReportRef = "matDemand",
                Params = new Dictionary<string, object> { ["plans"] = plans, ["lines"] = lines }
            };
        }

        // FC_GEM_STOCKOUT: รหัส stock ที่ cover < 30 วัน (quantity ÷ อัตราเบิกเฉลี่ย/วัน 90 วันล่าสุด)
        public static Wip.Finding? EvaluateFcGemStockout(int count, int days)
        {
            if (count <= 0) return null;

            return new Wip.Finding
            {
                Code = "FC_GEM_STOCKOUT",
                Severity = "warning",
                ReportRef = "matLowCover",
                Params = new Dictionary<string, object> { ["count"] = count, ["days"] = days }
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
            ("ACT_CHECK_WEIGHING", "deptHead", new[] { "GOLD_EXCESS_OVER_ALLOWANCE", "GOLD_MOST_WORKERS_OVER" }),
            ("ACT_ADD_WORKER", "productionManager", new[] { "CAP_QUEUE_BOTTLENECK" }),
            ("ACT_SPEED_COSTCARD", "goldControl", new[] { "CAP_COSTCARD_SLOW" }),
            ("ACT_CLEAN_STALE", "deptHead", new[] { "CAP_BACKLOG_MONTHS", "CAP_QUEUE_BOTTLENECK" }),
            ("ACT_SMOOTH_INFLOW", "planner", new[] { "CAP_INFLOW_OVER_OUTPUT", "FC_PEAK_RISK" }),
            ("ACT_RECEIVE_PENDING", "deptHead", new[] { "GOLD_STAGE_PENDING_RETURN" }),
            ("ACT_CHECK_STAGE", "productionManager", new[] { "GOLD_STAGE_ABOVE_TARGET" }),
            ("ACT_CROSS_TRAIN", "productionManager", new[] { "WRK_CONCENTRATION" }),
            ("ACT_REVIEW_RATE", "deptHead", new[] { "WRK_RATE_OUTLIER" }),
            ("ACT_RECORD_WAGES", "deptHead", new[] { "WRK_UNPAID_JOBS" }),
            ("ACT_TALK_WORKER_GOLD", "deptHead", new[] { "WRK_GOLD_REPEAT" }),
            ("ACT_ISSUE_READY", "deptHead", new[] { "MAT_READY_NOT_ISSUED" }),
            ("ACT_BUY_GEMS", "purchasing", new[] { "MAT_GEM_SHORT", "FC_GEM_SHORT_UPCOMING", "FC_GEM_STOCKOUT" }),
            ("ACT_FIX_GEM_SPEC", "productionManager", new[] { "MAT_SPEC_UNMATCHED" }),
            ("ACT_ADD_GEM_SORTER", "productionManager", new[] { "MAT_GEM_WAITING" })
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
                // ACT_ADD_WORKER/ACT_SPEED_COSTCARD/ACT_CLEAN_STALE/ACT_SMOOTH_INFLOW: ค่า fallback ที่นี่ใช้แค่กัน
                // พัง — ค่าจริงทั้งหมดถูกเติมทับจาก kpi/departments ตรงๆ ใน ProductionInsightService.EnrichCapacityActionParams
                // เสมอ (บทเรียนจาก ACT_REVIEW_ALLOWANCE ที่เคยพึ่ง finding param เดียวแล้วเป็น 0 หลอกๆ)
                case "ACT_ADD_WORKER":
                {
                    var deptsObj = GetParam(findings, "CAP_QUEUE_BOTTLENECK", "depts");
                    var deptsList = deptsObj as List<Dictionary<string, object>>;
                    var top = deptsList?.FirstOrDefault();
                    var deptKey = top != null && top.TryGetValue("deptKey", out var dk) ? dk : string.Empty;
                    return new Dictionary<string, object> { ["deptKey"] = deptKey ?? string.Empty, ["workersNow"] = 0, ["queueDaysNow"] = 0, ["queueDaysPlusOne"] = 0 };
                }
                case "ACT_SPEED_COSTCARD":
                {
                    var pendingActive = GetParam(findings, "CAP_COSTCARD_SLOW", "pendingActive") ?? 0;
                    var pendingNow = GetParam(findings, "CAP_COSTCARD_SLOW", "pendingNow") ?? 0;
                    var medianDays = GetParam(findings, "CAP_COSTCARD_SLOW", "medianDays") ?? 0;
                    return new Dictionary<string, object> { ["pendingActive"] = pendingActive, ["pendingNow"] = pendingNow, ["medianDays"] = medianDays };
                }
                case "ACT_CLEAN_STALE":
                    return new Dictionary<string, object> { ["staleWip"] = 0 };
                case "ACT_SMOOTH_INFLOW":
                {
                    var outputPerMonth = GetParam(findings, "CAP_INFLOW_OVER_OUTPUT", "outputPerMonth") ?? 0m;
                    return new Dictionary<string, object> { ["outputPerMonth"] = outputPerMonth };
                }
                // ACT_RECEIVE_PENDING/ACT_CHECK_STAGE: ค่า fallback ที่นี่ใช้แค่กันพัง — ค่าจริงถูกเติมทับจากข้อมูล
                // ที่คำนวณไว้แล้วตรงๆ ใน ProductionInsightService.EnrichGoldStageActionParams เสมอ
                case "ACT_RECEIVE_PENDING":
                {
                    var count = GetParam(findings, "GOLD_STAGE_PENDING_RETURN", "count") ?? 0;
                    var gram = GetParam(findings, "GOLD_STAGE_PENDING_RETURN", "gram") ?? 0m;
                    return new Dictionary<string, object> { ["count"] = count, ["gram"] = gram };
                }
                case "ACT_CHECK_STAGE":
                {
                    var deptKey = GetParam(findings, "GOLD_STAGE_ABOVE_TARGET", "deptKey") ?? string.Empty;
                    var diffPercent = GetParam(findings, "GOLD_STAGE_ABOVE_TARGET", "diffPercent") ?? 0m;
                    var targetPercent = GetParam(findings, "GOLD_STAGE_ABOVE_TARGET", "targetPercent") ?? 0m;
                    return new Dictionary<string, object> { ["deptKey"] = deptKey, ["diffPercent"] = diffPercent, ["targetPercent"] = targetPercent };
                }
                // ACT_CROSS_TRAIN/ACT_REVIEW_RATE/ACT_RECORD_WAGES/ACT_TALK_WORKER_GOLD: ค่า fallback ที่นี่ใช้แค่กัน
                // พัง (WRK_CONCENTRATION trigger ได้หลายครั้ง การหยิบ finding แรกตัวเดียวไม่ครบ) — ค่าจริงทั้งหมด
                // ถูกเติมทับจากข้อมูลที่คำนวณไว้แล้วตรงๆ ใน ProductionInsightService.EnrichWorkerActionParams เสมอ
                case "ACT_CROSS_TRAIN":
                {
                    var deptKey = GetParam(findings, "WRK_CONCENTRATION", "deptKey") ?? string.Empty;
                    var workersObj = GetParam(findings, "WRK_CONCENTRATION", "workers");
                    var workersList = workersObj as List<Dictionary<string, object>> ?? new List<Dictionary<string, object>>();
                    var workerNames = workersList.Select(w => w.TryGetValue("name", out var nm) ? nm : null).Where(nm => nm != null).ToList();
                    return new Dictionary<string, object> { ["deptKey"] = deptKey, ["workerNames"] = workerNames! };
                }
                case "ACT_REVIEW_RATE":
                {
                    var workersObj = GetParam(findings, "WRK_RATE_OUTLIER", "workers");
                    var workersList = workersObj as List<Dictionary<string, object>> ?? new List<Dictionary<string, object>>();
                    return new Dictionary<string, object> { ["workers"] = workersList };
                }
                case "ACT_RECORD_WAGES":
                {
                    var count = GetParam(findings, "WRK_UNPAID_JOBS", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_TALK_WORKER_GOLD":
                {
                    var workersObj = GetParam(findings, "WRK_GOLD_REPEAT", "workers");
                    var workersList = workersObj as List<Dictionary<string, object>> ?? new List<Dictionary<string, object>>();
                    return new Dictionary<string, object> { ["workers"] = workersList };
                }
                // ACT_ISSUE_READY/ACT_BUY_GEMS/ACT_FIX_GEM_SPEC/ACT_ADD_GEM_SORTER: ค่า fallback ที่นี่ใช้แค่กันพัง —
                // ค่าจริงถูกเติมทับจากข้อมูลที่คำนวณไว้แล้วตรงๆ ใน ProductionInsightService.EnrichMaterialActionParams เสมอ
                case "ACT_ISSUE_READY":
                {
                    var count = GetParam(findings, "MAT_READY_NOT_ISSUED", "count") ?? 0;
                    return new Dictionary<string, object> { ["count"] = count };
                }
                case "ACT_BUY_GEMS":
                {
                    var lines = GetParam(findings, "MAT_GEM_SHORT", "lines") ?? 0;
                    return new Dictionary<string, object> { ["lines"] = lines };
                }
                case "ACT_FIX_GEM_SPEC":
                {
                    var lines = GetParam(findings, "MAT_SPEC_UNMATCHED", "lines") ?? 0;
                    return new Dictionary<string, object> { ["lines"] = lines };
                }
                case "ACT_ADD_GEM_SORTER":
                    return new Dictionary<string, object> { ["deptKey"] = "gemSort" };
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
