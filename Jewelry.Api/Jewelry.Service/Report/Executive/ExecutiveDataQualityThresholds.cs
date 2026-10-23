using jewelry.Model.Report.Executive.DataQuality;
using System;

namespace Jewelry.Service.Report.Executive
{
    // ค่า threshold ของกระดานคุณภาพข้อมูล — default อยู่ที่นี่ที่เดียว, request.Thresholds override ได้ราย field
    public static class ExecutiveDataQualityThresholds
    {
        // ratio > warn = warning, ratio > critical = critical (strict: เท่ากับค่าพอดีไม่ผ่านเกณฑ์)
        public const decimal WarnPercent = 10m;
        public const decimal CriticalPercent = 50m;

        public static ThresholdValues Resolve(ThresholdOverrides? o)
        {
            var warn = ClampPercent(o?.WarnPercent ?? WarnPercent);
            var critical = Math.Max(warn, ClampPercent(o?.CriticalPercent ?? CriticalPercent));
            return new ThresholdValues { WarnPercent = warn, CriticalPercent = critical };
        }

        private static decimal ClampPercent(decimal v) => Math.Min(100m, Math.Max(0m, v));
    }
}
