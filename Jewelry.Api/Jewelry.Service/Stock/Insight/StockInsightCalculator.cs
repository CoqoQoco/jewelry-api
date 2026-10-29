using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Stock.Insight.Overview;

namespace Jewelry.Service.Stock.Insight
{
    public record StockMovementInput(DateTime MovementDateUtc, string MovementType, string? RefDocType, decimal Qty);

    // Pure — ไม่แตะ DB (unit-testable)
    public static class StockInsightCalculator
    {
        private const int ThaiOffsetHours = 7;
        public const string InvoiceRefDocType = "INVOICE";
        public const string InvoiceDeleteRefDocType = "INVOICE_DELETE";

        // วันแรกของเดือน (เวลาไทย) ของ instant UTC
        public static DateTime ThaiMonthStart(DateTime utc)
        {
            var th = utc.AddHours(ThaiOffsetHours);
            return new DateTime(th.Year, th.Month, 1);
        }

        // เดือนเรียงจากเก่าไปใหม่ จบที่เดือนปัจจุบัน (เวลาไทย)
        public static List<DateTime> MonthStarts(DateTime utcNow, int months)
        {
            var current = ThaiMonthStart(utcNow);
            return Enumerable.Range(0, months).Select(i => current.AddMonths(-(months - 1 - i))).ToList();
        }

        public static DateTime WindowStartUtc(DateTime firstThaiMonth)
            => DateTime.SpecifyKind(firstThaiMonth.AddHours(-ThaiOffsetHours), DateTimeKind.Utc);

        // ผลต่อยอดคงเหลือ qty: +1 เข้า / -1 ออก / 0 ไม่กระทบ (reserve, unreserve, ย้ายคลัง) — กติกากลางอยู่ที่ StockLedger
        public static int QtySign(string? movementType) => StockLedger.QtySign(movementType);

        public static bool IsLegacyImport(StockMovementInput m)
            => m.MovementType.Trim().ToUpperInvariant() == "RECEIPT"
               && StockLedger.IsLegacyImportRef(m.RefDocType);

        // endStock(เดือน m) = ยอดปัจจุบัน - ผลรวมสุทธิของ movement ที่เกิดหลังสิ้นเดือน m
        public static List<SeriesPoint> BuildSeries(IReadOnlyList<DateTime> months, IEnumerable<StockMovementInput> movements, decimal currentOnHandQty)
        {
            var points = months.Select(m => new SeriesPoint { Month = m.ToString("yyyy-MM-dd") }).ToList();
            var netByMonth = new decimal[months.Count];
            var indexByMonth = months.Select((m, i) => (m, i)).ToDictionary(x => x.m, x => x.i);

            foreach (var mv in movements)
            {
                var sign = QtySign(mv.MovementType);
                if (sign == 0) continue;
                if (!indexByMonth.TryGetValue(ThaiMonthStart(mv.MovementDateUtc), out var idx)) continue;

                var type = mv.MovementType.Trim().ToUpperInvariant();
                netByMonth[idx] += sign * mv.Qty;

                if (type == "SALE") points[idx].SaleQty += mv.Qty;
                else if (type == "RETURN" && string.Equals(mv.RefDocType, InvoiceDeleteRefDocType, StringComparison.OrdinalIgnoreCase))
                    points[idx].ReturnQty += mv.Qty;
                else if (type == "RECEIPT")
                {
                    if (IsLegacyImport(mv)) points[idx].LegacyImportQty += mv.Qty;
                    else points[idx].InflowQty += mv.Qty;
                }
                else points[idx].OtherNetQty += sign * mv.Qty;
            }

            foreach (var p in points) p.OutflowQty = Math.Max(p.SaleQty - p.ReturnQty, 0m);

            var after = 0m;
            for (var i = months.Count - 1; i >= 0; i--)
            {
                points[i].EndStockQty = currentOnHandQty - after;
                after += netByMonth[i];
            }

            return points;
        }

        // เดือนเต็ม (ก่อนเดือนปัจจุบัน) นับจากเดือนแรกที่มี SALE(INVOICE); startMonth null = 0
        public static int FullMonthsSince(DateTime? startMonth, DateTime currentMonth)
        {
            if (!startMonth.HasValue) return 0;
            var diff = (currentMonth.Year - startMonth.Value.Year) * 12 + currentMonth.Month - startMonth.Value.Month;
            return Math.Max(0, diff);
        }

        // เฉลี่ยยอดขายออกสุทธิ/เดือนจาก "เดือนเต็ม" ล่าสุด min(windowMonths, historyMonths) เดือน
        // (ตัดเดือนสุดท้าย = เดือนปัจจุบันที่ยังไม่จบ) คืน (avg, windowUsed)
        public static (decimal Avg, int WindowUsed) AverageMonthlyOutflow(IReadOnlyList<SeriesPoint> series, int windowMonths, int historyMonths)
        {
            var completed = series.Take(Math.Max(0, series.Count - 1)).ToList();
            var take = Math.Min(Math.Min(windowMonths, historyMonths), completed.Count);
            if (take <= 0) return (0m, 0);
            var window = completed.Skip(completed.Count - take).ToList();
            return (window.Sum(p => p.OutflowQty) / take, take);
        }

        // ไม่มี SALE เลยใน N เดือนล่าสุด (รวมเดือนปัจจุบัน) — series สั้นกว่า N = false
        public static bool HasNoOutflow(IReadOnlyList<SeriesPoint> series, int months)
        {
            if (series.Count < months) return false;
            return series.Skip(series.Count - months).All(p => p.OutflowQty == 0);
        }

        // อายุนับเป็นวันปฏิทินเวลาไทย (ageDate = วันเริ่มนับอายุของชิ้น ดู StockUnitSnapshot) · ว่าง = เก่าสุด
        public static string AgeBucketKey(DateTime? ageDate, DateTime utcNow)
        {
            if (!ageDate.HasValue) return "gt5y";
            var d = StockAge.ThaiDate(ageDate.Value);
            var today = StockAge.ThaiToday(utcNow);
            if (d >= today.AddYears(-1)) return "lt1y";
            if (d >= today.AddYears(-2)) return "y1to2";
            if (d >= today.AddYears(-5)) return "y2to5";
            return "gt5y";
        }

        // เดือนที่ (1..horizon) ที่ชิ้นอายุจะครบ 1 ปี — null ถ้าครบแล้ว (ไม่ใช่ชิ้นอายุน้อย) หรือเกิน horizon
        public static int? TurningMonthIndex(DateTime ageDate, DateTime utcNow, int horizonMonths)
        {
            var today = StockAge.ThaiToday(utcNow);
            var turn = StockAge.ThaiDate(ageDate).AddYears(1);
            if (turn < today) return null;
            for (var k = 1; k <= horizonMonths; k++)
            {
                if (turn <= today.AddMonths(k)) return k;
            }
            return null;
        }

        // คาดการณ์สัดส่วนของ "อายุเกิน 1 ปี" (ถ่วง qty) ในอีก horizon เดือน:
        //  - แต่ละเดือนขายออก avgMonthlyOutflow (ไม่เกินยอดคงเหลือ) แบบสัดส่วนเท่ากันทุกชิ้น (ชิ้นเก่า/ใหม่ลดลงเท่ากัน)
        //  - ไม่มีของใหม่เข้า; ชิ้นอายุน้อยที่ครบ 1 ปีในเดือนนั้น (turningByMonth[k-1], นับจากชุดเริ่มต้น) ย้ายเป็นชิ้นเก่า
        public static decimal ProjectAgedPercent(decimal totalQty, decimal agedQty, IReadOnlyList<decimal> turningByMonth, decimal avgMonthlyOutflow, int horizonMonths)
        {
            if (totalQty <= 0) return 0m;

            var aged = Math.Min(agedQty, totalQty);
            var buckets = new decimal[horizonMonths];
            var youngTotal = totalQty - aged;
            var turningSum = 0m;
            for (var i = 0; i < horizonMonths && i < turningByMonth.Count; i++)
            {
                buckets[i] = Math.Min(turningByMonth[i], youngTotal - turningSum);
                turningSum += buckets[i];
            }
            var rest = youngTotal - turningSum;

            for (var k = 1; k <= horizonMonths; k++)
            {
                var total = aged + rest + buckets.Sum();
                if (total <= 0) return 0m;

                var keep = 1m - Math.Min(avgMonthlyOutflow, total) / total;
                aged *= keep;
                rest *= keep;
                for (var i = 0; i < buckets.Length; i++) buckets[i] *= keep;

                aged += buckets[k - 1];
                buckets[k - 1] = 0m;
            }

            var end = aged + rest + buckets.Sum();
            return end <= 0 ? 0m : aged / end * 100m;
        }

        public static decimal Percent(decimal part, decimal whole)
            => whole <= 0 ? 0m : Math.Round(part * 100m / whole, 2, MidpointRounding.AwayFromZero);
    }
}
