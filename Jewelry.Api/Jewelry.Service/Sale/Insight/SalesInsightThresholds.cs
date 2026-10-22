using System;
using jewelry.Model.Sale.Insight.Overview;

namespace Jewelry.Service.Sale.Insight
{
    // ค่า threshold ของกฎ insight ขายและเงิน — default อยู่ที่นี่ที่เดียว, request.Thresholds override ได้ราย field
    public static class SalesInsightThresholds
    {
        // สัดส่วนยอดเลยกำหนดต่อยอดค้างชำระ (THB)
        public const decimal OverdueWarnPercent = 10m;
        public const decimal OverdueCriticalPercent = 25m;

        // บิลที่เลยกำหนดเก่าสุดเกินกี่วันถึงเตือน
        public const int OldestOverdueDays = 60;

        // สัดส่วนบิลค้างที่ไม่มี due date
        public const decimal NoDueDateWarnPercent = 20m;

        // ลูกค้ารายใหญ่สุดคิดเป็นกี่ % ของยอดค้าง
        public const decimal ConcentrationPercent = 40m;

        public const int CollectionWindowDays = 30;
        public const int BecomingOverdueDays = 7;
        public const int SoDeliveryDays = 14;
        public const int MinHistoryMonths = 3;

        public const int DefaultMonths = 12;
        public const int MinMonths = 3;
        public const int MaxMonths = 36;
        public const int MaxDays = 365;

        public static int ClampMonths(int months) => Math.Min(MaxMonths, Math.Max(MinMonths, months));

        public static ThresholdValues Resolve(ThresholdOverrides? o)
        {
            var overdueWarn = ClampPercent(o?.OverdueWarnPercent ?? OverdueWarnPercent);
            var overdueCritical = Math.Max(overdueWarn, ClampPercent(o?.OverdueCriticalPercent ?? OverdueCriticalPercent));

            return new ThresholdValues
            {
                OverdueWarnPercent = overdueWarn,
                OverdueCriticalPercent = overdueCritical,
                OldestOverdueDays = ClampDays(o?.OldestOverdueDays ?? OldestOverdueDays),
                NoDueDateWarnPercent = ClampPercent(o?.NoDueDateWarnPercent ?? NoDueDateWarnPercent),
                ConcentrationPercent = ClampPercent(o?.ConcentrationPercent ?? ConcentrationPercent),
                CollectionWindowDays = ClampDays(o?.CollectionWindowDays ?? CollectionWindowDays),
                BecomingOverdueDays = ClampDays(o?.BecomingOverdueDays ?? BecomingOverdueDays),
                SoDeliveryDays = ClampDays(o?.SoDeliveryDays ?? SoDeliveryDays),
                MinHistoryMonths = Math.Min(12, Math.Max(1, o?.MinHistoryMonths ?? MinHistoryMonths))
            };
        }

        private static decimal ClampPercent(decimal v) => Math.Min(100m, Math.Max(0m, v));

        private static int ClampDays(int v) => Math.Min(MaxDays, Math.Max(1, v));
    }
}
