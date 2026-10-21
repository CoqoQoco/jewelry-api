using System;
using jewelry.Model.Stock.Insight.Overview;

namespace Jewelry.Service.Stock.Insight
{
    // ค่า threshold ของกฎ insight คลังสินค้า — default อยู่ที่นี่ที่เดียว, request.Thresholds override ได้ราย field
    public static class StockInsightThresholds
    {
        public const decimal NoCostWarnPercent = 30m;
        public const decimal NoCostCriticalPercent = 60m;

        public const decimal Aged5yWarnPercent = 15m;
        public const decimal Aged5yCriticalPercent = 30m;

        // ไม่มี SALE ในกี่เดือนล่าสุด (รวมเดือนปัจจุบัน) ถึงเป็นปัญหา
        public const int NoOutflowMonths = 3;

        // ครอบคลุมกี่เดือนของยอดขายเฉลี่ย: ต่ำกว่า low = ของขาด, สูงกว่า high = ของล้น
        public const decimal CoverMonthsLow = 3m;
        public const decimal CoverMonthsHigh = 36m;

        // เฉลี่ยยอดขายออกต่อเดือนจากกี่เดือนเต็มล่าสุด (ไม่รวมเดือนปัจจุบันที่ยังไม่จบ)
        public const int CoverWindowMonths = 6;

        public const int AgingProjectionMonths = 6;
        public const decimal AgingRisePointsWarn = 5m;

        // ต้องมีประวัติขายจริงอย่างน้อยกี่เดือนเต็มถึงจะ emit cover/aging/no-outflow
        public const int MinHistoryMonths = 3;

        public const int DefaultMonths = 12;
        public const int MinMonths = 3;
        public const int MaxMonths = 36;

        public static int ClampMonths(int months) => Math.Min(MaxMonths, Math.Max(MinMonths, months));

        public static ThresholdValues Resolve(ThresholdOverrides? o)
        {
            var noCostWarn = ClampPercent(o?.NoCostWarnPercent ?? NoCostWarnPercent);
            var noCostCritical = Math.Max(noCostWarn, ClampPercent(o?.NoCostCriticalPercent ?? NoCostCriticalPercent));
            var agedWarn = ClampPercent(o?.Aged5yWarnPercent ?? Aged5yWarnPercent);
            var agedCritical = Math.Max(agedWarn, ClampPercent(o?.Aged5yCriticalPercent ?? Aged5yCriticalPercent));
            var coverLow = Math.Max(0m, o?.CoverMonthsLow ?? CoverMonthsLow);
            var coverHigh = Math.Max(coverLow, o?.CoverMonthsHigh ?? CoverMonthsHigh);

            return new ThresholdValues
            {
                NoCostWarnPercent = noCostWarn,
                NoCostCriticalPercent = noCostCritical,
                Aged5yWarnPercent = agedWarn,
                Aged5yCriticalPercent = agedCritical,
                NoOutflowMonths = Math.Min(MaxMonths, Math.Max(1, o?.NoOutflowMonths ?? NoOutflowMonths)),
                CoverMonthsLow = coverLow,
                CoverMonthsHigh = coverHigh,
                CoverWindowMonths = Math.Min(MaxMonths, Math.Max(1, o?.CoverWindowMonths ?? CoverWindowMonths)),
                AgingProjectionMonths = Math.Min(MaxMonths, Math.Max(1, o?.AgingProjectionMonths ?? AgingProjectionMonths)),
                AgingRisePointsWarn = Math.Max(0m, o?.AgingRisePointsWarn ?? AgingRisePointsWarn),
                MinHistoryMonths = Math.Min(12, Math.Max(1, o?.MinHistoryMonths ?? MinHistoryMonths))
            };
        }

        private static decimal ClampPercent(decimal v) => Math.Min(100m, Math.Max(0m, v));
    }
}
