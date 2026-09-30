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
    }
}
