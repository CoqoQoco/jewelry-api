using System;
using System.Collections.Generic;
using System.Linq;

namespace Jewelry.Service.Report.Executive
{
    // Pure — normalization / percent / median / lens helpers ของหน้า Flow (ไม่แตะ DB)
    // ค่าว่าง/ไม่รู้จัก = "Unknown" เสมอ (ไม่ถูกตัดทิ้ง) เพื่อให้ผลรวมของทุก lens เท่ากับยอดรวมของโซน
    public static class ExecutiveFlowNormalizer
    {
        public const string Unknown = "Unknown";

        public static readonly string[] PlanGoldOrder = { "YG", "WG", "PG", "SV", Unknown };

        // gold ของแผน/สินค้า (plan.type = tbm_gold.name_en หรือรหัส) -> YG | WG | PG | SV | Unknown
        public static string NormalizePlanGold(string? value)
        {
            var t = value?.Trim();
            if (string.IsNullOrEmpty(t)) return Unknown;
            if (Eq(t, "YG") || Eq(t, "Yellow Gold")) return "YG";
            if (Eq(t, "WG") || Eq(t, "White Gold")) return "WG";
            if (Eq(t, "PG") || Eq(t, "Pink Gold")) return "PG";
            if (Eq(t, "SV") || Eq(t, "Silver") || t.StartsWith("SIL", StringComparison.OrdinalIgnoreCase)) return "SV";
            return Unknown;
        }

        // size ของแผน/สินค้า -> 9K | 10K | 14K | 18K | Silver | Unknown  ('7' ในข้อมูลเดิม = 18K)
        public static string NormalizeSize(string? value)
        {
            var t = value?.Trim();
            if (string.IsNullOrEmpty(t)) return Unknown;
            if (t == "7") return "18K";
            if (Eq(t, "18K")) return "18K";
            if (Eq(t, "9K")) return "9K";
            if (Eq(t, "14K")) return "14K";
            if (Eq(t, "10K")) return "10K";
            if (Eq(t, "Silver")) return "Silver";
            return Unknown;
        }

        // lens "gold" ของสต็อก: WG/White Gold->White, YG/Yellow Gold->Yellow, Pink Gold->Pink, Silver->Silver, อื่นๆ->Unknown
        public static string NormalizeStockGold(string? value)
        {
            var t = value?.Trim();
            if (string.IsNullOrEmpty(t)) return Unknown;
            if (Eq(t, "WG") || Eq(t, "White Gold")) return "White";
            if (Eq(t, "YG") || Eq(t, "Yellow Gold")) return "Yellow";
            if (Eq(t, "Pink Gold")) return "Pink";
            if (Eq(t, "Silver")) return "Silver";
            return Unknown;
        }

        public static string LabelOrUnknown(string? value)
        {
            var t = value?.Trim();
            return string.IsNullOrEmpty(t) ? Unknown : t;
        }

        public static decimal? Percent(decimal part, decimal whole, int decimals = 1)
        {
            return whole > 0 ? Math.Round(part / whole * 100m, decimals, MidpointRounding.AwayFromZero) : (decimal?)null;
        }

        public static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        public static double? Median(IReadOnlyCollection<double> values)
        {
            if (values.Count == 0) return null;
            var sorted = values.OrderBy(v => v).ToList();
            var mid = sorted.Count / 2;
            var median = sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
            return Math.Round(median, 1);
        }

        // อายุเป็นวัน -> bucket 0:<=14, 1:15-60, 2:61-179, 3:>=180 (นับวันเต็ม)
        public static int AgeBucket(double days)
        {
            var d = (int)Math.Floor(days);
            if (d <= 14) return 0;
            if (d <= 60) return 1;
            if (d < 180) return 2;
            return 3;
        }

        public static readonly string[] StockAgeLabels = { "<=90d", "91-365d", ">365d", Unknown };

        public static string StockAgeLabel(DateTime? productionDate, DateTime now)
        {
            if (!productionDate.HasValue) return Unknown;
            var days = (now - productionDate.Value).TotalDays;
            if (days <= 90) return "<=90d";
            if (days <= 365) return "91-365d";
            return ">365d";
        }

        // เดือนปฏิทินเวลาไทยของ instant (UTC) -> yyyy-MM
        public static string ThaiMonthKey(DateTime utc) => utc.AddHours(7).ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);

        // top N ตามจำนวนมากสุด + แถวสุดท้ายรวมที่เหลือ; otherLabel รับ (จำนวนกลุ่มที่รวม) เพื่อสร้างป้าย
        public static List<(string Label, int Pcs)> TopWithOther(
            IEnumerable<(string Label, int Pcs)> items, int top, Func<int, string> otherLabel)
        {
            var ordered = items.OrderByDescending(i => i.Pcs).ThenBy(i => i.Label, StringComparer.Ordinal).ToList();
            if (ordered.Count <= top) return ordered;

            var head = ordered.Take(top).ToList();
            var rest = ordered.Skip(top).ToList();
            head.Add((otherLabel(rest.Count), rest.Sum(r => r.Pcs)));
            return head;
        }

        public static bool Reconciles(int total, params IEnumerable<int>[] lenses)
        {
            return lenses.All(l => l.Sum() == total);
        }

        private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
