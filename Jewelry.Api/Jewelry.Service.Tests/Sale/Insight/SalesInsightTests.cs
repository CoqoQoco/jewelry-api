using jewelry.Model.Sale.Insight.Overview;
using Jewelry.Service.Report.Executive;
using Jewelry.Service.Sale.Insight;
using Wip = jewelry.Model.Production.Insight.Wip;

namespace Jewelry.Service.Tests.Sale.Insight;

public class SalesInsightTests
{
    private static readonly ThresholdValues Defaults = SalesInsightThresholds.Resolve(null);

    // 2026-10-21 05:00 UTC = 12:00 ไทย
    private static readonly DateTime Now = new(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc);

    private static InvoiceMoneyRow Inv(string running, decimal grand, DateTime? due, string? code = "C1", string? name = "ลูกค้า 1",
        decimal paid = 0, decimal deposit = 0, decimal rate = 1, decimal share = 1, DateTime? create = null)
        => new()
        {
            Running = running, GrandTotal = grand, DueDate = due, CustomerCode = code, CustomerName = name, Paid = paid,
            Deposit = deposit, Rate = rate, Share = share, CreateDate = create ?? Now.AddDays(-5)
        };

    private static SalesOrderMoneyRow So(string no, decimal grand, DateTime? delivery, bool hasInvoice = false, decimal rate = 1, decimal share = 1)
        => new() { SoNumber = no, GrandTotal = grand, DeliveryDate = delivery, HasInvoice = hasInvoice, Rate = rate, Share = share };

    // ---- thresholds ----

    [Fact]
    public void Resolve_NullUsesDefaults()
    {
        Assert.Equal(10m, Defaults.OverdueWarnPercent);
        Assert.Equal(25m, Defaults.OverdueCriticalPercent);
        Assert.Equal(60, Defaults.OldestOverdueDays);
        Assert.Equal(20m, Defaults.NoDueDateWarnPercent);
        Assert.Equal(40m, Defaults.ConcentrationPercent);
        Assert.Equal(30, Defaults.CollectionWindowDays);
        Assert.Equal(7, Defaults.BecomingOverdueDays);
        Assert.Equal(14, Defaults.SoDeliveryDays);
        Assert.Equal(3, Defaults.MinHistoryMonths);
    }

    [Fact]
    public void Resolve_OverridesAndClamps()
    {
        var t = SalesInsightThresholds.Resolve(new ThresholdOverrides
        {
            OverdueWarnPercent = 50, OverdueCriticalPercent = 20, OldestOverdueDays = 0, CollectionWindowDays = 9999,
            ConcentrationPercent = 150, MinHistoryMonths = 99
        });
        Assert.Equal(50m, t.OverdueWarnPercent);
        Assert.Equal(50m, t.OverdueCriticalPercent);
        Assert.Equal(1, t.OldestOverdueDays);
        Assert.Equal(365, t.CollectionWindowDays);
        Assert.Equal(100m, t.ConcentrationPercent);
        Assert.Equal(12, t.MinHistoryMonths);
        Assert.Equal(7, t.BecomingOverdueDays);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(12, 12)]
    [InlineData(99, 36)]
    public void ClampMonths(int input, int expected) => Assert.Equal(expected, SalesInsightThresholds.ClampMonths(input));

    // ---- rules ----

    [Theory]
    [InlineData(9, null)]
    [InlineData(10, "warning")]
    [InlineData(24.9, "warning")]
    [InlineData(25, "critical")]
    public void OverdueReceivable_SeverityBoundaries(double overduePercent, string? expected)
    {
        var f = SalesInsightRuleEngine.EvaluateOverdueReceivable((decimal)overduePercent, 1, 100m, Defaults);
        Assert.Equal(expected, f?.Severity);
        if (f != null) { Assert.Equal("SAL_OVERDUE_RECEIVABLE", f.Code); Assert.Equal("salAging", f.ReportRef); }
    }

    [Fact]
    public void OverdueReceivable_NoOutstandingOrNoCount_Null()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateOverdueReceivable(0, 0, 0, Defaults));
        Assert.Null(SalesInsightRuleEngine.EvaluateOverdueReceivable(50, 0, 100, Defaults));
    }

    [Fact]
    public void OldestOverdue_WarnsOnlyAboveThreshold()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateOldestOverdue(60, "A", Defaults));
        Assert.Null(SalesInsightRuleEngine.EvaluateOldestOverdue(null, null, Defaults));
        var f = SalesInsightRuleEngine.EvaluateOldestOverdue(61, "A", Defaults)!;
        Assert.Equal("warning", f.Severity);
        Assert.Equal("salDue", f.ReportRef);
        Assert.Equal(61, f.Params["days"]);
        Assert.Equal("A", f.Params["customerName"]);
    }

    [Fact]
    public void NoDueDate_BoundaryAndZeroUnpaid()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateNoDueDate(1, 6, Defaults));
        Assert.Equal("warning", SalesInsightRuleEngine.EvaluateNoDueDate(1, 5, Defaults)!.Severity);
        Assert.Null(SalesInsightRuleEngine.EvaluateNoDueDate(0, 0, Defaults));
    }

    [Fact]
    public void SoNotInvoiced_WarningWhenAny_NeverCritical()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateSoNotInvoiced(0, 0, 0, 0));
        var f = SalesInsightRuleEngine.EvaluateSoNotInvoiced(3, 249800m, 1, 2)!;
        Assert.Equal("warning", f.Severity);
        Assert.Equal("salSo", f.ReportRef);
        Assert.Equal(1, f.Params["overdueDeliveryCount"]);
        Assert.Equal(2, f.Params["noDeliveryDateCount"]);
    }

    [Fact]
    public void Concentration_Boundary()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateConcentration("A", 39.99m, 100, Defaults));
        Assert.Null(SalesInsightRuleEngine.EvaluateConcentration("A", 90m, 0, Defaults));
        Assert.Null(SalesInsightRuleEngine.EvaluateConcentration(null, null, 100, Defaults));
        Assert.Equal("warning", SalesInsightRuleEngine.EvaluateConcentration("A", 40m, 100, Defaults)!.Severity);
    }

    [Fact]
    public void Collection_InfoWhenDue_WarningWhenNoneDue_NullWhenNoOutstanding()
    {
        Assert.Equal("info", SalesInsightRuleEngine.EvaluateCollection(100, 1, 500, 30)!.Severity);
        Assert.Equal("warning", SalesInsightRuleEngine.EvaluateCollection(0, 0, 500, 30)!.Severity);
        Assert.Null(SalesInsightRuleEngine.EvaluateCollection(0, 0, 0, 30));
    }

    [Fact]
    public void BecomingOverdueAndSoDelivery_WarnWhenCountPositive()
    {
        Assert.Null(SalesInsightRuleEngine.EvaluateBecomingOverdue(0, 0, 7));
        Assert.Equal("warning", SalesInsightRuleEngine.EvaluateBecomingOverdue(10, 1, 7)!.Severity);
        Assert.Null(SalesInsightRuleEngine.EvaluateSoDelivery(0, 0, 14));
        Assert.Equal("warning", SalesInsightRuleEngine.EvaluateSoDelivery(1, 10, 14)!.Severity);
    }

    [Fact]
    public void WorstSeverity_InfoDoesNotCount()
    {
        Assert.Equal("ok", SalesInsightRuleEngine.WorstSeverity(new List<Wip.Finding>()));
        Assert.Equal("ok", SalesInsightRuleEngine.WorstSeverity(new[] { new Wip.Finding { Code = "X", Severity = "info" } }));
        Assert.Equal("warning", SalesInsightRuleEngine.WorstSeverity(new[] { new Wip.Finding { Code = "X", Severity = "warning" } }));
        Assert.Equal("critical", SalesInsightRuleEngine.WorstSeverity(new[]
        {
            new Wip.Finding { Code = "X", Severity = "warning" }, new Wip.Finding { Code = "Y", Severity = "critical" }
        }));
    }

    // ---- actions ----

    [Fact]
    public void Actions_CriticalFirst_ThenDeclarationOrder_OwnerSaleManager()
    {
        var problems = new List<Wip.Finding>
        {
            SalesInsightRuleEngine.EvaluateNoDueDate(5, 5, Defaults)!,
            SalesInsightRuleEngine.EvaluateSoNotInvoiced(2, 100, 0, 0)!,
            SalesInsightRuleEngine.EvaluateOverdueReceivable(30, 2, 100, Defaults)!
        };
        var actions = SalesInsightRuleEngine.BuildActions(problems, new List<Wip.Finding>(), "ลูกค้า 1");

        Assert.Equal(new[] { "ACT_FOLLOW_OVERDUE", "ACT_SET_DUE_DATE", "ACT_INVOICE_FROM_SO" }, actions.Select(a => a.Code).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, actions.Select(a => a.Priority).ToArray());
        Assert.All(actions, a => Assert.Equal("saleManager", a.OwnerRole));
        Assert.Equal("ลูกค้า 1", actions[0].Params["topCustomerName"]);
        Assert.Equal(2, actions[0].Params["overdueCount"]);
        Assert.Contains("FC_SAL_BECOMING_OVERDUE", actions[0].RelatedCodes);
    }

    [Fact]
    public void Actions_CriticalRelatedOutranksDeclarationOrder()
    {
        var problems = new List<Wip.Finding>
        {
            SalesInsightRuleEngine.EvaluateOverdueReceivable(10, 1, 100, Defaults)!,      // warning
            SalesInsightRuleEngine.EvaluateConcentration("A", 90m, 100, Defaults)!        // warning
        };
        var forecasts = new List<Wip.Finding> { SalesInsightRuleEngine.EvaluateSoDelivery(1, 5, 14)! };
        var actions = SalesInsightRuleEngine.BuildActions(problems, forecasts);
        Assert.Equal(new[] { "ACT_FOLLOW_OVERDUE", "ACT_INVOICE_FROM_SO", "ACT_SPREAD_RISK" }, actions.Select(a => a.Code).ToArray());

        var withCritical = new List<Wip.Finding>
        {
            SalesInsightRuleEngine.EvaluateSoNotInvoiced(1, 1, 0, 0)!,
            new() { Code = "SAL_CUSTOMER_CONCENTRATION", Severity = "critical", Params = new() { ["customerName"] = "A", ["percent"] = 90m } }
        };
        var ordered = SalesInsightRuleEngine.BuildActions(withCritical, new List<Wip.Finding>());
        Assert.Equal(new[] { "ACT_SPREAD_RISK", "ACT_INVOICE_FROM_SO" }, ordered.Select(a => a.Code).ToArray());
    }

    [Fact]
    public void Actions_EmptyWhenNoFindings()
        => Assert.Empty(SalesInsightRuleEngine.BuildActions(new List<Wip.Finding>(), new List<Wip.Finding>()));

    // ---- aging ----

    [Theory]
    [InlineData(9999, "notDue")]     // 9999 = ไม่มี due date
    [InlineData(10, "notDue")]     // due ในอนาคต (+10 วัน)
    [InlineData(-0.5, "d1_30")]
    [InlineData(-1, "d1_30")]
    [InlineData(-30, "d1_30")]
    [InlineData(-31, "d31_60")]
    [InlineData(-60, "d31_60")]
    [InlineData(-61, "d61_90")]
    [InlineData(-90, "d61_90")]
    [InlineData(-91, "gt90")]
    public void AgingKey_Buckets(double dueOffsetDays, string expected)
    {
        DateTime? due = dueOffsetDays > 9000 ? null : Now.AddDays(dueOffsetDays);
        Assert.Equal(expected, SalesInsightCalculator.AgingKey(due, Now));
    }

    [Fact]
    public void BuildAging_AlwaysFiveBuckets_SumsThb()
    {
        var unpaid = new List<InvoiceMoneyRow>
        {
            Inv("a", 100, null),
            Inv("b", 200, Now.AddDays(5)),
            Inv("c", 300, Now.AddDays(-45), rate: 2),
            Inv("d", 400, Now.AddDays(-200))
        };
        var aging = SalesInsightCalculator.BuildAging(unpaid, Now);
        Assert.Equal(SalesInsightCalculator.AgingKeys, aging.Select(a => a.Key).ToArray());
        Assert.Equal(2, aging[0].Count);
        Assert.Equal(300m, aging[0].AmountThb);
        Assert.Equal(0, aging[1].Count);
        Assert.Equal(600m, aging[2].AmountThb);
        Assert.Equal(400m, aging[4].AmountThb);
    }

    // ---- top customers / concentration ----

    [Fact]
    public void TopCustomers_GroupByCodeFallbackName_SortedAndLimited()
    {
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("1", 100, Now.AddDays(-5), "A", "Alpha"),
            Inv("2", 300, Now.AddDays(10), "A", "Alpha"),
            Inv("3", 250, null, "B", "Beta"),
            Inv("4", 50, null, null, "NoCode"),
            Inv("5", 10, null, "", "NoCode")
        };
        var top = SalesInsightCalculator.BuildTopCustomers(rows, Now);
        Assert.Equal(new[] { "Alpha", "Beta", "NoCode" }, top.Select(x => x.CustomerName).ToArray());
        Assert.Equal(400m, top[0].OutstandingThb);
        Assert.Equal(100m, top[0].OverdueThb);
        Assert.Equal(2, top[0].Count);
        Assert.Equal(2, top[2].Count);

        var many = Enumerable.Range(0, 15).Select(i => Inv($"x{i}", 10 + i, null, $"C{i}", $"N{i}")).ToList();
        Assert.Equal(10, SalesInsightCalculator.BuildTopCustomers(many, Now).Count);
    }

    [Fact]
    public void Build_Concentration_UsesTopShareOfOutstanding()
    {
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("1", 600, Now.AddDays(10), "A", "Alpha"),
            Inv("2", 400, Now.AddDays(10), "B", "Beta")
        };
        var r = SalesInsightCalculator.Build("ALL", 12, Defaults, Now, rows, new List<SalesOrderMoneyRow>());
        Assert.Equal(60m, r.Kpi.TopCustomerPercent);
        var f = Assert.Single(r.Problems, p => p.Code == "SAL_CUSTOMER_CONCENTRATION");
        Assert.Equal("Alpha", f.Params["customerName"]);
    }

    // ---- collection windows ----

    [Fact]
    public void Build_CollectionWindows()
    {
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("due3", 100, Now.AddDays(3)),
            Inv("due20", 200, Now.AddDays(20)),
            Inv("due31", 400, Now.AddDays(31)),
            Inv("late", 800, Now.AddDays(-2)),
            Inv("nodue", 1600, null)
        };
        var so = new List<SalesOrderMoneyRow>
        {
            So("S1", 50, Now.AddDays(5)),
            So("S2", 60, Now.AddDays(15)),
            So("S3", 70, Now.AddDays(-1)),
            So("S4", 80, null),
            So("S5", 90, Now.AddDays(5), hasInvoice: true)
        };
        var r = SalesInsightCalculator.Build("ALL", 12, Defaults, Now, rows, so);

        Assert.Equal(300m, r.Collection.DueNextWindowThb);
        Assert.Equal(2, r.Collection.DueNextWindowCount);
        Assert.Equal(100m, r.Collection.BecomingOverdueThb);
        Assert.Equal(1, r.Collection.BecomingOverdueCount);
        Assert.Equal(1, r.Collection.SoDeliveryDueCount);
        Assert.Equal(50m, r.Collection.SoDeliveryDueThb);
        Assert.Equal(4, r.Kpi.SoNotInvoicedCount);
        Assert.Equal(1, r.Kpi.SoDeliveryOverdueCount);
        Assert.Equal(1, r.Kpi.SoNoDeliveryDateCount);
        Assert.Equal(2, r.Kpi.OldestOverdueDays);
        Assert.Contains(r.Forecasts, f => f.Code == "FC_SAL_COLLECTION" && f.Severity == "info");
        Assert.Contains(r.Forecasts, f => f.Code == "FC_SAL_BECOMING_OVERDUE");
        Assert.Contains(r.Forecasts, f => f.Code == "FC_SAL_SO_DELIVERY");
    }

    // ---- series / history (Thai time) ----

    [Fact]
    public void Series_UsesThaiMonth_OldestToNewest()
    {
        // 2026-09-30 18:00 UTC = 2026-10-01 01:00 ไทย → ต้องตกเดือน ต.ค.
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("a", 100, null, create: new DateTime(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc)),
            Inv("b", 50, null, create: new DateTime(2026, 9, 30, 16, 59, 0, DateTimeKind.Utc)),
            Inv("c", 10, null, create: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        };
        var r = SalesInsightCalculator.Build("ALL", 3, Defaults, Now, rows, new List<SalesOrderMoneyRow>());
        Assert.Equal(new[] { "2026-08-01", "2026-09-01", "2026-10-01" }, r.Series.Select(s => s.Month).ToArray());
        Assert.Equal(new[] { 0, 1, 1 }, r.Series.Select(s => s.InvoiceCount).ToArray());
        Assert.Equal(100m, r.Series[2].GrandTotalThb);
        Assert.Equal(50m, r.Series[1].GrandTotalThb);
    }

    [Fact]
    public void SalesHistory_StartMonthAndFullMonths()
    {
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("a", 1, null, create: new DateTime(2026, 2, 10, 3, 0, 0, DateTimeKind.Utc)),
            Inv("b", 1, null, create: new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc))
        };
        var h = SalesInsightCalculator.BuildSalesHistory(rows, Now);
        Assert.Equal("2026-02-01", h.StartMonth);
        Assert.Equal(8, h.FullMonths);

        var none = SalesInsightCalculator.BuildSalesHistory(new List<InvoiceMoneyRow>(), Now);
        Assert.Null(none.StartMonth);
        Assert.Equal(0, none.FullMonths);
    }

    // ---- edge cases ----

    [Fact]
    public void Build_EmptyInputs_NoExceptions_StatusOk()
    {
        var r = SalesInsightCalculator.Build("ALL", 12, Defaults, Now, new List<InvoiceMoneyRow>(), new List<SalesOrderMoneyRow>());
        Assert.Equal("ok", r.Status);
        Assert.Empty(r.Problems);
        Assert.Empty(r.Forecasts);
        Assert.Empty(r.Actions);
        Assert.Equal(0m, r.Kpi.OverduePercent);
        Assert.Equal(0m, r.Kpi.PaidPercent);
        Assert.Null(r.Kpi.OldestOverdueDays);
        Assert.Null(r.Kpi.TopCustomerPercent);
        Assert.Equal(3, r.PaymentStates.Count);
        Assert.Equal(5, r.Aging.Count);
        Assert.Equal(12, r.Series.Count);
    }

    [Fact]
    public void Build_AllPaid_NoOutstanding_NoFindings()
    {
        var rows = new List<InvoiceMoneyRow> { Inv("a", 100, Now.AddDays(-100), paid: 100), Inv("b", 50, null, deposit: 50) };
        var r = SalesInsightCalculator.Build("ALL", 12, Defaults, Now, rows, new List<SalesOrderMoneyRow>());
        Assert.Equal(0, r.Kpi.OutstandingCount);
        Assert.Equal(100m, r.Kpi.PaidPercent);
        Assert.Equal("ok", r.Status);
        Assert.Empty(r.Problems);
    }

    [Fact]
    public void Build_NullCustomer_DoesNotThrow()
    {
        var rows = new List<InvoiceMoneyRow> { Inv("a", 100, Now.AddDays(-70), null, null) };
        var r = SalesInsightCalculator.Build("ALL", 12, Defaults, Now, rows, new List<SalesOrderMoneyRow>());
        Assert.Single(r.TopCustomers);
        Assert.Equal(70, r.Kpi.OldestOverdueDays);
        Assert.Equal("critical", r.Status);
    }

    // ---- PARITY กับ ExecutiveReport.Summary / Receivables ----

    [Fact]
    public void Parity_WithSummaryReceivablesMath_AllAndMetalShare()
    {
        var rows = new List<InvoiceMoneyRow>
        {
            Inv("1", 1000, Now.AddDays(-10), rate: 1),                         // unpaid overdue
            Inv("2", 2000, Now.AddDays(10), rate: 35, share: 0.5m),            // unpaid ยังไม่ถึงกำหนด, USD, share 0.5
            Inv("3", 500, null, share: 0.25m),                                 // unpaid ไม่มี due
            Inv("4", 700, Now.AddDays(-3), paid: 100),                         // partial
            Inv("5", 900, Now.AddDays(-3), deposit: 900),                      // มัดจำ
            Inv("6", 0, null),                                                 // ยอด 0 ไม่ใช่ unpaid
            Inv("7", 300, Now.AddDays(-1), paid: 300)                          // paid
        };

        // Summary เรียก SummarizeReceivables ตัวเดียวกัน — ตรวจเทียบกับสูตรเดิม (inline ก่อน refactor) แบบคำนวณมือ
        var s = ExecutiveSalesMath.SummarizeReceivables(rows, Now);
        var unpaid = rows.Where(x => x.GrandTotal > 0 && x.Paid == 0 && x.Deposit <= 0).ToList();
        var overdue = unpaid.Where(x => x.DueDate.HasValue && x.DueDate.Value < Now).ToList();
        Assert.Equal(rows.Sum(x => x.GrandTotal * x.Rate * x.Share), s.InvoiceTotalThb);
        Assert.Equal(unpaid.Sum(x => x.GrandTotal * x.Rate * x.Share), s.UnpaidThb);
        Assert.Equal(overdue.Sum(x => x.GrandTotal * x.Rate * x.Share), s.OverdueThb);

        var r = SalesInsightCalculator.Build("GOLD", 12, Defaults, Now, rows, new List<SalesOrderMoneyRow>());
        Assert.Equal(s.InvoiceCount, r.Kpi.TotalInvoiceCount);
        Assert.Equal(Math.Round(s.InvoiceTotalThb, 2), r.Kpi.TotalGrandThb);
        Assert.Equal(s.UnpaidCount, r.Kpi.OutstandingCount);
        Assert.Equal(Math.Round(s.UnpaidThb, 2), r.Kpi.OutstandingThb);
        Assert.Equal(s.OverdueCount, r.Kpi.OverdueCount);
        Assert.Equal(Math.Round(s.OverdueThb, 2), r.Kpi.OverdueThb);
        Assert.Equal(s.NoDueDateCount, r.Kpi.NoDueDateAllCount);

        // โดนัท 3 slice = [paidOrPartialThb, unpaidNotOverdueThb, overdueThb] ของ Summary
        Assert.Equal(new[] { "received", "unpaidNotDue", "overdue" }, r.PaymentStates.Select(p => p.Key).ToArray());
        Assert.Equal(Math.Round(s.PaidOrPartialThb, 2), r.PaymentStates[0].AmountThb);
        Assert.Equal(Math.Round(s.UnpaidNotOverdueThb, 2), r.PaymentStates[1].AmountThb);
        Assert.Equal(Math.Round(s.OverdueThb, 2), r.PaymentStates[2].AmountThb);
        Assert.Equal(rows.Count, r.PaymentStates.Sum(p => p.Count));
        Assert.Equal(r.Kpi.OutstandingThb, r.Aging.Sum(a => a.AmountThb));

        // Receivables ใช้ IsUnpaidStrict เดียวกัน: unpaid = 3 บิล (1,2,3)
        Assert.Equal(3, s.UnpaidCount);
        Assert.Equal(1, s.OverdueCount);
    }

    [Fact]
    public void Parity_SalesOrdersWithoutInvoice_MatchesSummarySoTotals()
    {
        var so = new List<SalesOrderMoneyRow>
        {
            So("S1", 100, Now.AddDays(-1)),
            So("S2", 200, null, share: 0.5m),
            So("S3", 300, Now.AddDays(3), hasInvoice: true)
        };
        var t = ExecutiveSalesMath.SummarizeSalesOrders(so, Now);
        var r = SalesInsightCalculator.Build("GOLD", 12, Defaults, Now, new List<InvoiceMoneyRow>(), so);
        Assert.Equal(3, t.OpenCount);
        Assert.Equal(t.NoInvoiceCount, r.Kpi.SoNotInvoicedCount);
        Assert.Equal(t.NoInvoiceThb, r.Kpi.SoNotInvoicedThb);
        Assert.Equal(200m, r.Kpi.SoNotInvoicedThb);
        Assert.Equal(t.OverdueNoInvoiceCount, r.Kpi.SoDeliveryOverdueCount);
        Assert.Equal(t.NoDeliveryDateCount, r.Kpi.SoNoDeliveryDateCount);
    }
}
