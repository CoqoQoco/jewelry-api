using System;
using System.Collections.Generic;
using System.Linq;
using jewelry.Model.Sale.Insight.Overview;
using Jewelry.Service.Report.Executive;
using Jewelry.Service.Stock.Insight;

namespace Jewelry.Service.Sale.Insight
{
    // Pure — ไม่แตะ DB (unit-testable); ตัวเลขเงินใช้ ExecutiveSalesMath ชุดเดียวกับ ExecutiveReport.Summary
    public static class SalesInsightCalculator
    {
        public static readonly string[] AgingKeys = { "notDue", "d1_30", "d31_60", "d61_90", "gt90" };

        public const int TopCustomerLimit = 10;

        // จำนวนวันที่เลยกำหนด (ตัดทศนิยม) — สูตรเดียวกับ Receivables.DaysOverdue
        public static int DaysOverdue(DateTime dueDate, DateTime now) => (int)(now - dueDate).TotalDays;

        // เลยกำหนดวันนี้ (น้อยกว่า 1 วัน) นับเป็น d1_30; ไม่มี due date หรือยังไม่ถึงกำหนด = notDue
        public static string AgingKey(DateTime? dueDate, DateTime now)
        {
            if (!dueDate.HasValue || dueDate.Value >= now) return "notDue";
            var days = DaysOverdue(dueDate.Value, now);
            if (days <= 30) return "d1_30";
            if (days <= 60) return "d31_60";
            if (days <= 90) return "d61_90";
            return "gt90";
        }

        public static decimal Percent(decimal part, decimal whole)
            => whole <= 0 ? 0m : Math.Round(part * 100m / whole, 2, MidpointRounding.AwayFromZero);

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        private static string CustomerKey(InvoiceMoneyRow x)
            => !string.IsNullOrWhiteSpace(x.CustomerCode) ? x.CustomerCode!.Trim() : (x.CustomerName ?? string.Empty).Trim();

        public static List<AgingBucket> BuildAging(IReadOnlyList<InvoiceMoneyRow> unpaidRows, DateTime now)
        {
            var keyed = unpaidRows.ToLookup(x => AgingKey(x.DueDate, now));
            return AgingKeys.Select(key =>
            {
                var list = keyed[key].ToList();
                return new AgingBucket { Key = key, Count = list.Count, AmountThb = Round2(list.Sum(ExecutiveSalesMath.Thb)) };
            }).ToList();
        }

        // ลูกค้าที่มียอดค้างสูงสุด take รายการ — จัดกลุ่มด้วย customer_code (ว่าง = ใช้ชื่อ)
        public static List<TopCustomer> BuildTopCustomers(IReadOnlyList<InvoiceMoneyRow> unpaidRows, DateTime now, int take = TopCustomerLimit)
        {
            return unpaidRows
                .GroupBy(CustomerKey)
                .Select(g => new
                {
                    Code = g.Select(x => x.CustomerCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                    Name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                    Count = g.Count(),
                    Outstanding = g.Sum(ExecutiveSalesMath.Thb),
                    Overdue = g.Where(x => ExecutiveSalesMath.IsOverdue(x, now)).Sum(ExecutiveSalesMath.Thb)
                })
                .OrderByDescending(x => x.Outstanding)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Take(take)
                .Select(x => new TopCustomer
                {
                    CustomerCode = x.Code,
                    CustomerName = x.Name,
                    Count = x.Count,
                    OutstandingThb = Round2(x.Outstanding),
                    OverdueThb = Round2(x.Overdue)
                })
                .ToList();
        }

        // ลูกค้าที่มียอด "เลยกำหนด" สูงสุด (สำหรับ action ติดตามหนี้) — null ถ้าไม่มี
        public static string? TopOverdueCustomerName(IReadOnlyList<InvoiceMoneyRow> unpaidRows, DateTime now)
        {
            return unpaidRows
                .Where(x => ExecutiveSalesMath.IsOverdue(x, now))
                .GroupBy(CustomerKey)
                .Select(g => new
                {
                    Name = g.Select(x => x.CustomerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? g.Key,
                    Amount = g.Sum(ExecutiveSalesMath.Thb)
                })
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.Name, StringComparer.Ordinal)
                .Select(x => x.Name)
                .FirstOrDefault();
        }

        // ครบกำหนดภายใน days วันข้างหน้า: now <= due <= now + days (เลยกำหนดแล้วไม่นับ)
        public static bool IsDueWithin(DateTime? due, DateTime now, int days)
            => due.HasValue && due.Value >= now && due.Value <= now.AddDays(days);

        public static List<SeriesPoint> BuildSeries(IReadOnlyList<DateTime> monthStarts, IEnumerable<InvoiceMoneyRow> rows)
        {
            var points = monthStarts.Select(m => new SeriesPoint { Month = m.ToString("yyyy-MM-dd") }).ToList();
            var sums = new decimal[monthStarts.Count];
            var index = monthStarts.Select((m, i) => (m, i)).ToDictionary(x => x.m, x => x.i);

            foreach (var row in rows)
            {
                if (!index.TryGetValue(StockInsightCalculator.ThaiMonthStart(row.CreateDate), out var idx)) continue;
                points[idx].InvoiceCount++;
                sums[idx] += ExecutiveSalesMath.Thb(row);
            }

            for (var i = 0; i < points.Count; i++) points[i].GrandTotalThb = Round2(sums[i]);
            return points;
        }

        public static SalesHistory BuildSalesHistory(IReadOnlyList<InvoiceMoneyRow> rows, DateTime now)
        {
            if (rows.Count == 0) return new SalesHistory();

            var start = StockInsightCalculator.ThaiMonthStart(rows.Min(x => x.CreateDate));
            return new SalesHistory
            {
                StartMonth = start.ToString("yyyy-MM-dd"),
                FullMonths = StockInsightCalculator.FullMonthsSince(start, StockInsightCalculator.ThaiMonthStart(now))
            };
        }

        // ประกอบ response ทั้งหมดจากแถวที่โหลดแล้ว (pure) — invoiceRows/soRows ผ่านตัวกรองโลหะแล้ว
        public static Response Build(
            string metal, int months, ThresholdValues t, DateTime now,
            IReadOnlyList<InvoiceMoneyRow> invoiceRows, IReadOnlyList<SalesOrderMoneyRow> soRows)
        {
            var totals = ExecutiveSalesMath.SummarizeReceivables(invoiceRows, now);
            var soTotals = ExecutiveSalesMath.SummarizeSalesOrders(soRows, now);

            var unpaidRows = invoiceRows.Where(ExecutiveSalesMath.IsUnpaidStrict).ToList();
            var overdueRows = unpaidRows.Where(x => ExecutiveSalesMath.IsOverdue(x, now)).ToList();
            var noDueDateUnpaid = unpaidRows.Count(x => !x.DueDate.HasValue);

            var oldest = overdueRows
                .OrderByDescending(x => DaysOverdue(x.DueDate!.Value, now))
                .ThenByDescending(ExecutiveSalesMath.Thb)
                .FirstOrDefault();
            int? oldestDays = oldest == null ? null : DaysOverdue(oldest.DueDate!.Value, now);

            var topCustomers = BuildTopCustomers(unpaidRows, now);
            decimal? topPercent = null;
            string? topName = null;
            if (totals.UnpaidThb > 0 && topCustomers.Count > 0)
            {
                // ใช้ค่าไม่ปัด (คำนวณใหม่จากแถวจริง) กันเพี้ยนจากการปัด 2 ตำแหน่งของยอดต่อลูกค้า
                var top = unpaidRows.GroupBy(CustomerKey).Select(g => new { g.Key, Amount = g.Sum(ExecutiveSalesMath.Thb) })
                    .OrderByDescending(x => x.Amount).First();
                topPercent = top.Amount * 100m / totals.UnpaidThb;
                topName = topCustomers[0].CustomerName;
            }

            var dueNext = unpaidRows.Where(x => IsDueWithin(x.DueDate, now, t.CollectionWindowDays)).ToList();
            var becoming = unpaidRows.Where(x => IsDueWithin(x.DueDate, now, t.BecomingOverdueDays)).ToList();
            var soDue = soRows.Where(x => !x.HasInvoice && IsDueWithin(x.DeliveryDate, now, t.SoDeliveryDays)).ToList();
            var soDueThb = soDue.Sum(x => x.GrandTotal * x.Rate * x.Share);

            var collection = new Collection
            {
                DueNextWindowThb = Round2(dueNext.Sum(ExecutiveSalesMath.Thb)),
                DueNextWindowCount = dueNext.Count,
                BecomingOverdueThb = Round2(becoming.Sum(ExecutiveSalesMath.Thb)),
                BecomingOverdueCount = becoming.Count,
                SoDeliveryDueCount = soDue.Count,
                SoDeliveryDueThb = Round2(soDueThb)
            };

            var problems = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                SalesInsightRuleEngine.EvaluateOverdueReceivable(totals.OverdueThb, totals.OverdueCount, totals.UnpaidThb, t),
                SalesInsightRuleEngine.EvaluateOldestOverdue(oldestDays, oldest?.CustomerName, t),
                SalesInsightRuleEngine.EvaluateNoDueDate(noDueDateUnpaid, totals.UnpaidCount, t),
                SalesInsightRuleEngine.EvaluateSoNotInvoiced(soTotals.NoInvoiceCount, soTotals.NoInvoiceThb, soTotals.OverdueNoInvoiceCount, soTotals.NoDeliveryDateCount),
                SalesInsightRuleEngine.EvaluateConcentration(topName, topPercent, totals.UnpaidThb, t)
            }.Where(f => f != null).Select(f => f!).ToList();

            var forecasts = new List<jewelry.Model.Production.Insight.Wip.Finding?>
            {
                SalesInsightRuleEngine.EvaluateCollection(dueNext.Sum(ExecutiveSalesMath.Thb), dueNext.Count, totals.UnpaidThb, t.CollectionWindowDays),
                SalesInsightRuleEngine.EvaluateBecomingOverdue(becoming.Sum(ExecutiveSalesMath.Thb), becoming.Count, t.BecomingOverdueDays),
                SalesInsightRuleEngine.EvaluateSoDelivery(soDue.Count, soDueThb, t.SoDeliveryDays)
            }.Where(f => f != null).Select(f => f!).ToList();

            var monthStarts = StockInsightCalculator.MonthStarts(now, months);

            return new Response
            {
                AsOf = now,
                Metal = metal,
                Status = SalesInsightRuleEngine.WorstSeverity(problems.Concat(forecasts)),
                Thresholds = t,
                Kpi = new Kpi
                {
                    TotalInvoiceCount = totals.InvoiceCount,
                    TotalGrandThb = Round2(totals.InvoiceTotalThb),
                    OutstandingCount = totals.UnpaidCount,
                    OutstandingThb = Round2(totals.UnpaidThb),
                    OverdueCount = totals.OverdueCount,
                    OverdueThb = Round2(totals.OverdueThb),
                    OverduePercent = Percent(totals.OverdueThb, totals.UnpaidThb),
                    PaidPercent = Percent(totals.PaidOrPartialThb, totals.InvoiceTotalThb),
                    NoDueDateCount = noDueDateUnpaid,
                    NoDueDatePercent = Percent(noDueDateUnpaid, totals.UnpaidCount),
                    NoDueDateAllCount = totals.NoDueDateCount,
                    OldestOverdueDays = oldestDays,
                    TopCustomerPercent = topPercent.HasValue ? Round2(topPercent.Value) : null,
                    SoNotInvoicedCount = soTotals.NoInvoiceCount,
                    SoNotInvoicedThb = Round2(soTotals.NoInvoiceThb),
                    SoDeliveryOverdueCount = soTotals.OverdueNoInvoiceCount,
                    SoNoDeliveryDateCount = soTotals.NoDeliveryDateCount
                },
                PaymentStates = new List<PaymentState>
                {
                    new PaymentState { Key = "received", Count = totals.PaidOrPartialCount, AmountThb = Round2(totals.PaidOrPartialThb) },
                    new PaymentState { Key = "unpaidNotDue", Count = totals.UnpaidNotOverdueCount, AmountThb = Round2(totals.UnpaidNotOverdueThb) },
                    new PaymentState { Key = "overdue", Count = totals.OverdueCount, AmountThb = Round2(totals.OverdueThb) }
                },
                Aging = BuildAging(unpaidRows, now),
                TopCustomers = topCustomers,
                Collection = collection,
                Series = BuildSeries(monthStarts, invoiceRows),
                SalesHistory = BuildSalesHistory(invoiceRows, now),
                Problems = problems,
                Forecasts = forecasts,
                Actions = SalesInsightRuleEngine.BuildActions(problems, forecasts, TopOverdueCustomerName(unpaidRows, now))
            };
        }
    }
}
