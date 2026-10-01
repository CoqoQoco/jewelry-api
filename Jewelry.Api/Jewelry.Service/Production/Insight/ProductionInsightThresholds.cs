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
    }
}
