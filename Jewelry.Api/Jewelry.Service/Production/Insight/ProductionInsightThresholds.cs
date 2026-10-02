namespace Jewelry.Service.Production.Insight
{
    // ค่า threshold ของกฎ insight ทั้งหมด รวมไว้ที่เดียวเพื่อย้ายไปเป็น DB setting ทีหลังได้ง่าย
    public static class ProductionInsightThresholds
    {
        public const int DefaultStaleDays = 180;
        public const int DefaultRiskWindowDays = 30;
        public const int DefaultFlowRangeDays = 90;

        // WIP_DEPT_GROWING: deltaPercent >= ค่านี้ ถือว่า trigger (warning), >= 2 เท่า ถือว่า critical
        public const int DefaultGrowthThresholdPercent = 20;

        // WIP_STALE: percent ของ stale ต่อ open ทั้งหมด >= ค่านี้ ถือว่า critical (ไม่ถึง = warning)
        public const int WipStalePercentCritical = 10;

        // WIP_OVERDUE: percent ของ overdue ต่อ open ทั้งหมด >= ค่านี้ ถือว่า critical (ไม่ถึง = warning)
        public const int WipOverduePercentCritical = 20;

        // FC_DUE_SOON_AT_RISK: จำนวนแผนที่เสี่ยง >= ค่านี้ ถือว่า critical (ไม่ถึง = warning)
        public const int FcDueSoonAtRiskCountCritical = 20;

        // ค่ามาตรฐานเริ่มต้น (วัน) ถ้าแผนกไหนไม่มี saved standard เลย (กันพัง ไม่ควรเกิดจริงเพราะ seed migration ใส่ไว้ครบ)
        public const decimal DefaultStageStandardDays = 14;

        // dwell ปัจจุบัน > ตัวคูณนี้ × standard ถือว่า "ผิดปกติ" (ใช้ทั้ง StageLeadTime.abnormalCount และ
        // STAGE_ABNORMAL_DWELL — AbnormalDwellPlans endpoint รับ multiplier จาก request ได้ ค่า default เดียวกัน)
        public const decimal DefaultAbnormalDwellMultiplier = 2;

        // STAGE_OVER_STANDARD: (median-standard)/standard*100 >= ค่านี้ ถือว่า critical (ไม่ถึง = warning)
        public const int StageOverStandardPercentCritical = 50;

        // STAGE_WAIT_DOMINANT: waitDays/(waitDays+workDays) >= ค่านี้ % ถือว่า wait ครอบงำ
        public const int StageWaitDominantSharePercent = 50;

        // FC_STAGE_LEADTIME_RISING: จำนวน bucket ที่ "ผ่านเกณฑ์" (qualifying) ล่าสุดที่ต้องเรียงเพิ่มขึ้นต่อเนื่อง
        // (เทียบ bucket ต่อ bucket แบบ strictly increasing) — bucket ที่ count < MinSamplesPerBucket ถูกข้ามไปเลย
        // ไม่ถูกนับเป็นหนึ่งใน 3 ตัวนี้ (กัน bucket ว่าง medianTotal=0 หลอกว่า "เพิ่มขึ้น")
        public const int StageLeadtimeRisingBucketCount = 3;

        // bucket ที่มี exited visit น้อยกว่านี้ ถือว่าข้อมูลไม่พอ (ไม่ใช่ "medianTotal=0" จริง) — ข้ามจากการเช็ค
        // FC_STAGE_LEADTIME_RISING และแสดงเป็น null ใน StageLeadTime.series แทนการโชว์ 0 หลอกตา
        public const int MinSamplesPerBucket = 3;

        // STAGE_WAIT_DOMINANT: ต้องมี SplitSampleCount (exited visit ที่แยก wait/work ได้จริงจาก receive_date)
        // อย่างน้อยเท่านี้ถึงจะเชื่อถือได้ — กันแจ้งเตือนจากข้อมูลน้อยเกินไปตอนเพิ่งเริ่มเก็บ receive_date
        public const int MinSplitSamples = 10;

        // DLV_STUCK_AFTER_COSTCARD: จำนวนแผนที่ค้างหลังบัตรต้นทุน (เคยเข้า 95, ไม่เคยถึง 100, สถานะปัจจุบันไม่ใช่
        // 500/84/85) >= ค่านี้ ถือว่า critical (ไม่ถึง = warning)
        public const int DlvStuckAfterCostCardCountCritical = 50;

        // ค่าเป้าหมาย % ส่งตรงเวลา เริ่มต้น ถ้าตาราง tbt_production_delivery_target ว่างเปล่าจริงๆ (กันพังเฉยๆ
        // ไม่ควรเกิดจริงเพราะ seed migration ใส่ไว้แล้ว)
        public const decimal DefaultDeliveryTargetPercent = 80;

        // ---- Gold Loss (ทองและ Loss) ----

        // GOLD_EXCESS_OVER_ALLOWANCE: ส่วนเกิน allowance รวม (กรัม) ต่อประเภทช่าง > ค่านี้ ถือว่า critical
        public const decimal GoldExcessOverAllowanceCriticalGram = 50;

        // GOLD_LOSS_ABOVE_TARGET: lossPercent >= targetPercent × ค่านี้ ถือว่า critical (เกินแต่ไม่ถึง = warning)
        public const decimal GoldLossAboveTargetCriticalMultiplier = 1.5m;

        // GOLD_MOST_WORKERS_OVER: % ช่างที่เกิน allowance ของตัวเอง >= ค่านี้ ถือว่า trigger, >= ค่า critical ด้านล่าง = critical
        public const int GoldMostWorkersOverPercent = 70;
        public const int GoldMostWorkersOverPercentCritical = 90;

        // GOLD_REPEAT_OFFENDER: ช่างที่เกิน allowance ทุก bucket ที่ผ่านเกณฑ์ (qualifying) ติดต่อกันอย่างน้อยเท่านี้
        // ถึง trigger (warning), >= ค่า critical ด้านล่าง = critical
        public const int GoldRepeatOffenderMinBuckets = 3;
        public const int GoldRepeatOffenderCriticalBuckets = 6;

        // GOLD_SLIP_COVERAGE_LOW: % งานที่ถูกตัดใบ gold loss ไปแล้ว < ค่านี้ ถือว่า trigger (warning), < ค่า critical ด้านล่าง = critical
        public const int GoldSlipCoverageLowPercent = 80;
        public const int GoldSlipCoverageLowPercentCritical = 50;

        // ค่าเป้าหมาย % เสียทอง เริ่มต้นต่อประเภทช่าง×โลหะ ถ้าตาราง tbt_production_gold_loss_target ว่างเปล่า
        // จริงๆ (กันพังเฉยๆ ไม่ควรเกิดจริงเพราะ seed migration ใส่ไว้แล้วครบ 4 ชุด) — ตรงกับค่า seed ล่าสุด
        public const decimal DefaultGoldLossTargetPercentTangGold = 2.2m;
        public const decimal DefaultGoldLossTargetPercentSettingGold = 3.4m;
        public const decimal DefaultGoldLossTargetPercentTangSilver = 2.5m;
        public const decimal DefaultGoldLossTargetPercentSettingSilver = 2.3m;

        // GOLD_LOSS_ABOVE_TARGET / GOLD_ALLOWANCE_ABOVE_TARGET: ต้องเกิน target ด้วยมากกว่าค่านี้ถึง trigger
        // (กันแจ้งเตือนจากส่วนต่างเล็กน้อยระดับ rounding error)
        public const decimal GoldTargetTolerancePercent = 0.1m;

        // ---- Capacity (กำลังการผลิต) ----

        // CAP_BACKLOG_MONTHS: activeWip ÷ outputPerMonth (เดือน) > ค่านี้ถึง trigger (warning), > critical = critical
        public const decimal CapBacklogMonthsWarn = 2m;
        public const decimal CapBacklogMonthsCritical = 4m;

        // CAP_QUEUE_BOTTLENECK: จำนวนแผนกคิวยาวสุดที่ใส่ใน params.depts
        public const int CapQueueBottleneckTopCount = 2;

        // CAP_INFLOW_OVER_OUTPUT: จำนวนเดือนในช่วงที่ inflow > output ต้อง >= ค่านี้ถึง trigger
        public const int CapOverloadMonthsThreshold = 2;

        // CAP_COSTCARD_SLOW: median 95→100 (วัน) > ค่านี้ หรือ pendingOver30d > ค่านี้ ถึง trigger (OR เงื่อนไข)
        public const int CapCostCardSlowMedianDaysThreshold = 7;
        public const int CapCostCardSlowPendingOver30dThreshold = 50;

        // pendingOver30d (ทั้งใน kpi.costCardToDone และ CAP_COSTCARD_SLOW) — "ค้างนาน" = เข้า 95 มาแล้วเกินกี่วัน
        public const int CapCostCardPendingOver30dDays = 30;

        // FC_BACKLOG_PROJECTED: จำนวนเดือนที่ใช้ project ไปข้างหน้า
        public const int FcBacklogProjectedMonths = 3;

        // FC_PEAK_RISK: inflow เดือนพีค > ค่านี้ × output ถึง trigger
        public const decimal FcPeakRiskMultiplier = 1.5m;

        // ---- Gold Loss by Stage (Loss ตามใบงานรายแผนก จ่าย-รับ) ----

        // outlier: diff% ของแถว > ค่านี้ × median diff% ของแผนก (เฉพาะแถวคืนแล้วในช่วง) — ปรับจาก 2× เป็น 3×
        // (2× ให้ 291 แถวใน 3 เดือน สัญญาณรบกวนเยอะเกินไป)
        public const decimal GoldStageOutlierMedianMultiplier = 3m;

        // outlier: diffGram ของแถวต้อง >= ค่านี้ (กรัม) ด้วย (กันแถวเล็กจิ๋วที่ % สูงลิบแต่กรัมแทบไม่มีนัยสำคัญ) —
        // ปรับจาก 0.10g เป็น 0.20g
        public const decimal GoldStageOutlierMinDiffGram = 0.20m;

        // GOLD_STAGE_PENDING_RETURN / GoldStagePendingReturn default: งานที่ยังไม่คืนเกินกี่วันถือว่า "ค้าง"
        public const int GoldStagePendingOlderThanDaysDefault = 14;

        // GOLD_STAGE_OUTLIER_JOBS: จำนวน outlier รวมทุกแผนก (ที่ไม่ยกเว้น) >= ค่านี้ถึง trigger
        public const int GoldStageOutlierJobsCountThreshold = 5;

        // GOLD_STAGE_ABOVE_TARGET: ต้องเกิน target ด้วยมากกว่าค่านี้ถึง trigger (เหมือน GoldTargetTolerancePercent)
        public const decimal GoldStageTargetTolerancePercent = 0.1m;

        // worker breakdown ของ GoldByStage.departments[].workers: ต้องมีงาน >= เท่านี้แถวถึงนับ, top กี่คน
        public const int GoldStageWorkerMinRows = 5;
        public const int GoldStageWorkerTopCount = 10;

        // ---- ช่างและค่าแรง (Workers) ----

        // WRK_CONCENTRATION: แผนกต้องมีช่างจริง (ไม่รวม placeholder/TEST) >= เท่านี้คน และ top2Share (0-100) >= เท่านี้ถึง trigger
        public const int WrkConcentrationMinWorkers = 3;
        public const decimal WrkConcentrationTop2SharePercent = 50m;

        // WRK_RATE_OUTLIER: wagePerJob > ค่านี้ × median ของกลุ่มแผนก+ประเภทการจ้างเดียวกัน และต้องมีงาน >= เท่านี้ชิ้น
        public const decimal WrkRateOutlierMultiplier = 3m;
        public const int WrkRateOutlierMinJobs = 5;

        // WRK_WAGE_PER_PLAN_RISING/FC_WAGES_NEXT_MONTH: bucket เดือนที่สั้นกว่านี้ (วัน) ไม่ถือว่าเป็น "เต็มเดือน"
        public const double WrkFullMonthMinDays = 28;
    }
}
