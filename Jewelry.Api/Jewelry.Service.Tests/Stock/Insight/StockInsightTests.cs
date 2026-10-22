using jewelry.Model.Stock.Insight.Overview;
using Jewelry.Service.Stock.Insight;

namespace Jewelry.Service.Tests.Stock.Insight;

public class StockInsightTests
{
    private static readonly ThresholdValues Defaults = StockInsightThresholds.Resolve(null);

    // ---- thresholds ----

    [Fact]
    public void Resolve_NullUsesDefaults()
    {
        Assert.Equal(30m, Defaults.NoCostWarnPercent);
        Assert.Equal(60m, Defaults.NoCostCriticalPercent);
        Assert.Equal(15m, Defaults.Aged5yWarnPercent);
        Assert.Equal(30m, Defaults.Aged5yCriticalPercent);
        Assert.Equal(3, Defaults.NoOutflowMonths);
        Assert.Equal(3m, Defaults.CoverMonthsLow);
        Assert.Equal(36m, Defaults.CoverMonthsHigh);
        Assert.Equal(6, Defaults.CoverWindowMonths);
        Assert.Equal(6, Defaults.AgingProjectionMonths);
        Assert.Equal(5m, Defaults.AgingRisePointsWarn);
        Assert.Equal(3, Defaults.MinHistoryMonths);
    }

    [Fact]
    public void Resolve_OverridesOnlyGivenFields_AndClamps()
    {
        var t = StockInsightThresholds.Resolve(new ThresholdOverrides { NoCostWarnPercent = 10, NoCostCriticalPercent = 5, CoverWindowMonths = 0, MinHistoryMonths = 99 });
        Assert.Equal(10m, t.NoCostWarnPercent);
        Assert.Equal(10m, t.NoCostCriticalPercent);
        Assert.Equal(1, t.CoverWindowMonths);
        Assert.Equal(12, t.MinHistoryMonths);
        Assert.Equal(15m, t.Aged5yWarnPercent);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(12, 12)]
    [InlineData(99, 36)]
    public void ClampMonths(int input, int expected) => Assert.Equal(expected, StockInsightThresholds.ClampMonths(input));

    // ---- problems ----

    [Theory]
    [InlineData(29, 100, null)]
    [InlineData(30, 100, "warning")]
    [InlineData(59, 100, "warning")]
    [InlineData(60, 100, "critical")]
    public void NoCostHigh_SeverityBoundaries(int noCost, int total, string? expected)
    {
        var f = StockInsightRuleEngine.EvaluateNoCostHigh(noCost, total, Defaults);
        Assert.Equal(expected, f?.Severity);
        if (f != null)
        {
            Assert.Equal("STK_NO_COST_HIGH", f.Code);
            Assert.Equal("stkCost", f.ReportRef);
            Assert.Equal(noCost, f.Params["noCostCount"]);
            Assert.Equal(total, f.Params["inStockCount"]);
        }
    }

    [Fact]
    public void NoCostHigh_ZeroStock_NoFinding() => Assert.Null(StockInsightRuleEngine.EvaluateNoCostHigh(0, 0, Defaults));

    [Theory]
    [InlineData(14, null)]
    [InlineData(15, "warning")]
    [InlineData(30, "critical")]
    public void AgedOver5y_SeverityBoundaries(int count, string? expected)
        => Assert.Equal(expected, StockInsightRuleEngine.EvaluateAgedOver5y(count, 100, Defaults)?.Severity);

    [Fact]
    public void NoProductionDate_OnlyWhenPositive()
    {
        Assert.Null(StockInsightRuleEngine.EvaluateNoProductionDate(0));
        Assert.Equal("stkAge", StockInsightRuleEngine.EvaluateNoProductionDate(2)!.ReportRef);
    }

    [Fact]
    public void NoOutflow_RequiresStock()
    {
        Assert.Null(StockInsightRuleEngine.EvaluateNoOutflow(true, 0, 4, Defaults));
        Assert.Null(StockInsightRuleEngine.EvaluateNoOutflow(false, 10, 4, Defaults));
        var f = StockInsightRuleEngine.EvaluateNoOutflow(true, 10, 4, Defaults)!;
        Assert.Equal(3, f.Params["months"]);
        Assert.Equal(4, f.Params["historyMonths"]);
        Assert.Null(StockInsightRuleEngine.EvaluateNoOutflow(true, 10, 2, Defaults));
    }

    // ---- forecasts ----

    [Fact]
    public void Cover_InsideRange_NoFinding() => Assert.Null(StockInsightRuleEngine.EvaluateCoverMonths(100, 10, 4, 4, Defaults));

    [Fact]
    public void Cover_Low_WarningThenCritical()
    {
        var warn = StockInsightRuleEngine.EvaluateCoverMonths(20, 10, 4, 4, Defaults)!;
        Assert.Equal("warning", warn.Severity);
        Assert.Equal("low", warn.Params["direction"]);
        Assert.Equal("critical", StockInsightRuleEngine.EvaluateCoverMonths(10, 10, 4, 4, Defaults)!.Severity);
    }

    [Fact]
    public void Cover_High_AndZeroOutflowDoesNotDivide()
    {
        var high = StockInsightRuleEngine.EvaluateCoverMonths(1000, 10, 4, 4, Defaults)!;
        Assert.Equal("high", high.Params["direction"]);

        var zero = StockInsightRuleEngine.EvaluateCoverMonths(50, 0, 4, 4, Defaults)!;
        Assert.Equal("high", zero.Params["direction"]);
        Assert.Null(zero.Params["coverMonths"]);

        Assert.Null(StockInsightRuleEngine.EvaluateCoverMonths(0, 0, 4, 4, Defaults));
    }

    [Fact]
    public void Cover_InsufficientHistory_NoFinding_AndCarriesHistoryParams()
    {
        Assert.Null(StockInsightRuleEngine.EvaluateCoverMonths(1000, 0, 2, 2, Defaults));
        var f = StockInsightRuleEngine.EvaluateCoverMonths(20, 10, 4, 3, Defaults)!;
        Assert.Equal(4, f.Params["historyMonths"]);
        Assert.Equal(3, f.Params["windowMonths"]);
    }

    [Fact]
    public void AgingProjected_OnlyWhenRiseAtLeastThreshold()
    {
        Assert.Null(StockInsightRuleEngine.EvaluateAgingProjected(40, 44.9m, 4, 4, Defaults));
        var f = StockInsightRuleEngine.EvaluateAgingProjected(40, 45, 4, 4, Defaults)!;
        Assert.Equal("FC_STK_AGING_PROJECTED", f.Code);
        Assert.Equal(6, f.Params["months"]);
        Assert.Equal(4, f.Params["historyMonths"]);
        Assert.Null(StockInsightRuleEngine.EvaluateAgingProjected(40, 60, 2, 2, Defaults));
    }

    // ---- calculator ----

    [Fact]
    public void ProjectAgedPercent_NoOutflow_AddsTurningPieces()
    {
        // total 100, aged 40, 10 turn in month 2, no outflow -> 50%
        var to = StockInsightCalculator.ProjectAgedPercent(100, 40, new decimal[] { 0, 10, 0 }, 0, 3);
        Assert.Equal(50m, to);
    }

    [Fact]
    public void ProjectAgedPercent_ProportionalOutflow_KeepsShareWhenNothingTurns()
    {
        var to = StockInsightCalculator.ProjectAgedPercent(100, 40, new decimal[] { 0, 0 }, 10, 2);
        Assert.Equal(40m, Math.Round(to, 6));
    }

    [Fact]
    public void ProjectAgedPercent_OutflowLargerThanStock_IsSafe()
        => Assert.Equal(0m, StockInsightCalculator.ProjectAgedPercent(5, 2, new decimal[] { 0 }, 100, 1));

    [Fact]
    public void TurningMonthIndex_Boundaries()
    {
        var now = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(StockInsightCalculator.TurningMonthIndex(now.AddYears(-1).AddDays(-1), now, 6));
        Assert.Equal(1, StockInsightCalculator.TurningMonthIndex(now.AddYears(-1).AddDays(10), now, 6));
        Assert.Equal(3, StockInsightCalculator.TurningMonthIndex(now.AddYears(-1).AddMonths(3), now, 6));
        Assert.Null(StockInsightCalculator.TurningMonthIndex(now.AddYears(-1).AddMonths(7), now, 6));
    }

    [Fact]
    public void AgeBucketKey_MatchesStockHealth()
    {
        var now = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        Assert.Equal("gt5y", StockInsightCalculator.AgeBucketKey(null, now));
        Assert.Equal("lt1y", StockInsightCalculator.AgeBucketKey(now.AddMonths(-6), now));
        Assert.Equal("y1to2", StockInsightCalculator.AgeBucketKey(now.AddMonths(-18), now));
        Assert.Equal("y2to5", StockInsightCalculator.AgeBucketKey(now.AddYears(-3), now));
        Assert.Equal("gt5y", StockInsightCalculator.AgeBucketKey(now.AddYears(-6), now));
    }

    // ---- series ----

    private static StockMovementInput Mv(string type, string utc, decimal qty, string? refDoc = null)
        => new(DateTime.SpecifyKind(DateTime.Parse(utc), DateTimeKind.Utc), type, refDoc, qty);

    [Fact]
    public void MonthStarts_UsesThaiTime()
    {
        // 2026-09-30 18:00 UTC = 2026-10-01 01:00 Thai -> current month is October
        var now = new DateTime(2026, 9, 30, 18, 0, 0, DateTimeKind.Utc);
        var months = StockInsightCalculator.MonthStarts(now, 3);
        Assert.Equal(new DateTime(2026, 8, 1), months[0]);
        Assert.Equal(new DateTime(2026, 10, 1), months[2]);
    }

    [Fact]
    public void BuildSeries_ThaiMonthBoundary_AndEndStockReconstruction()
    {
        var months = new List<DateTime> { new(2026, 8, 1), new(2026, 9, 1), new(2026, 10, 1) };
        var movements = new[]
        {
            Mv("RECEIPT", "2026-08-10T00:00:00", 10),
            Mv("RECEIPT", "2026-08-31T18:00:00", 4),             // = 1 Sep Thai -> September
            Mv("RECEIPT", "2026-09-05T00:00:00", 100, "TRANSFER_9K"), // legacy import
            Mv("SALE", "2026-09-20T00:00:00", 3),
            Mv("SALE", "2026-10-02T00:00:00", 2),
            Mv("RETURN", "2026-10-03T00:00:00", 1),
            Mv("TRANSFORM_OUT", "2026-10-04T00:00:00", 1),
            Mv("TRANSFORM_IN", "2026-10-04T00:00:00", 1),
            Mv("RESERVE", "2026-10-04T00:00:00", 5),
            Mv("Transfer", "2026-10-04T00:00:00", 5)
        };

        var s = StockInsightCalculator.BuildSeries(months, movements, 500);

        Assert.Equal(10, s[0].InflowQty);
        Assert.Equal(4, s[1].InflowQty);
        Assert.Equal(100, s[1].LegacyImportQty);
        Assert.Equal(3, s[1].SaleQty);
        Assert.Equal(3, s[1].OutflowQty);
        Assert.Equal(2, s[2].SaleQty);
        Assert.Equal(2, s[2].OutflowQty);
        Assert.Equal(0, s[2].ReturnQty);   // RETURN ในเคสนี้ไม่มี RefDocType INVOICE_DELETE
        Assert.Equal(1, s[2].OtherNetQty); // RETURN(ไม่ใช่ลบบิล) 1 + TRANSFORM_IN 1 - TRANSFORM_OUT 1

        // October net = -2 +1 -1 +1 = -1 ; September net = 4+100-3 = 101
        Assert.Equal(500, s[2].EndStockQty);
        Assert.Equal(501, s[1].EndStockQty);
        Assert.Equal(400, s[0].EndStockQty);
    }

    [Fact]
    public void AverageMonthlyOutflow_ExcludesCurrentMonth_AndUsesWindowAndHistory()
    {
        var series = new List<SeriesPoint>
        {
            new() { OutflowQty = 100 }, new() { OutflowQty = 6 }, new() { OutflowQty = 12 }, new() { OutflowQty = 999 }
        };
        Assert.Equal((9m, 2), StockInsightCalculator.AverageMonthlyOutflow(series, 2, 10));
        Assert.Equal((9m, 2), StockInsightCalculator.AverageMonthlyOutflow(series, 6, 2));
        Assert.Equal((0m, 0), StockInsightCalculator.AverageMonthlyOutflow(series, 6, 0));
        Assert.Equal((0m, 0), StockInsightCalculator.AverageMonthlyOutflow(new List<SeriesPoint> { new() { OutflowQty = 5 } }, 6, 6));
    }

    [Fact]
    public void FullMonthsSince_CountsCompletedMonthsOnly()
    {
        var cur = new DateTime(2026, 10, 1);
        Assert.Equal(4, StockInsightCalculator.FullMonthsSince(new DateTime(2026, 6, 1), cur));
        Assert.Equal(0, StockInsightCalculator.FullMonthsSince(cur, cur));
        Assert.Equal(0, StockInsightCalculator.FullMonthsSince(null, cur));
    }

    [Fact]
    public void NetOutflow_FromProdSeptemberNumbers_AvgAbout162()
    {
        var months = new List<DateTime> { new(2026, 5, 1), new(2026, 6, 1), new(2026, 7, 1), new(2026, 8, 1), new(2026, 9, 1), new(2026, 10, 1) };
        var movements = new[]
        {
            Mv("SALE", "2026-06-10T00:00:00", 197, "INVOICE"), Mv("RETURN", "2026-06-11T00:00:00", 129, "INVOICE_DELETE"),
            Mv("SALE", "2026-07-10T00:00:00", 22, "INVOICE"),  Mv("RETURN", "2026-07-11T00:00:00", 5, "INVOICE_DELETE"),
            Mv("SALE", "2026-08-10T00:00:00", 20, "INVOICE"),  Mv("RETURN", "2026-08-11T00:00:00", 16, "INVOICE_DELETE"),
            Mv("SALE", "2026-09-10T00:00:00", 1186, "INVOICE"), Mv("RETURN", "2026-09-11T00:00:00", 626, "INVOICE_DELETE"),
            Mv("SALE", "2026-10-10T00:00:00", 276, "INVOICE"), Mv("RETURN", "2026-10-11T00:00:00", 45, "INVOICE_DELETE")
        };

        var s = StockInsightCalculator.BuildSeries(months, movements, 1000);

        Assert.Equal(new decimal[] { 68, 17, 4, 560 }, s.Skip(1).Take(4).Select(p => p.OutflowQty).ToArray());
        Assert.Equal(1186, s[4].SaleQty);
        Assert.Equal(626, s[4].ReturnQty);
        Assert.Equal(0, s[4].OtherNetQty);
        Assert.Equal(0, s[0].OutflowQty);

        // ledger identity: endStock เดือนก่อนหน้า = ปัจจุบัน - net หลังเดือนนั้น (ใช้ signed ledger เต็ม)
        Assert.Equal(1000, s[5].EndStockQty);
        Assert.Equal(1000 + (276 - 45), s[4].EndStockQty); // ตุลาคมขายสุทธิ 231 -> สิ้นกันยายนมากกว่าปัจจุบัน 231

        var history = StockInsightCalculator.FullMonthsSince(new DateTime(2026, 6, 1), months[5]);
        Assert.Equal(4, history);
        var (avg, used) = StockInsightCalculator.AverageMonthlyOutflow(s, 6, history);
        Assert.Equal(4, used);
        Assert.Equal(162.25m, avg);
    }

    [Fact]
    public void NetOutflow_FlooredAtZero_WhenReturnsExceedSales()
    {
        var months = new List<DateTime> { new(2026, 9, 1), new(2026, 10, 1) };
        var s = StockInsightCalculator.BuildSeries(months, new[]
        {
            Mv("SALE", "2026-09-10T00:00:00", 5, "INVOICE"), Mv("RETURN", "2026-09-11T00:00:00", 8, "INVOICE_DELETE")
        }, 50);
        Assert.Equal(0, s[0].OutflowQty);
        Assert.Equal(8, s[0].ReturnQty);
        Assert.Equal(50, s[0].EndStockQty);
        Assert.Equal(50, s[1].EndStockQty);
    }

    [Fact]
    public void HasNoOutflow_LooksAtTrailingMonthsIncludingCurrent()
    {
        var series = new List<SeriesPoint> { new() { OutflowQty = 5 }, new(), new(), new() };
        Assert.True(StockInsightCalculator.HasNoOutflow(series, 3));
        series[3].OutflowQty = 1;
        Assert.False(StockInsightCalculator.HasNoOutflow(series, 3));
        Assert.False(StockInsightCalculator.HasNoOutflow(new List<SeriesPoint> { new(), new() }, 3));
    }

    // ---- actions / status ----

    [Fact]
    public void BuildActions_LowCoverGivesReplenish_HighGivesSlowIntake()
    {
        var low = StockInsightRuleEngine.EvaluateCoverMonths(10, 10, 4, 4, Defaults)!;
        var actions = StockInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding>(), new[] { low });
        Assert.Single(actions);
        Assert.Equal("ACT_REPLENISH", actions[0].Code);

        var high = StockInsightRuleEngine.EvaluateCoverMonths(1000, 1, 4, 4, Defaults)!;
        var actions2 = StockInsightRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding>(), new[] { high });
        Assert.Equal("ACT_SLOW_INTAKE", Assert.Single(actions2).Code);
    }

    [Fact]
    public void BuildActions_PriorityByWorstSeverityThenDeclaredOrder_AndStatus()
    {
        var noCost = StockInsightRuleEngine.EvaluateNoCostHigh(50, 100, Defaults)!;            // warning
        var aged = StockInsightRuleEngine.EvaluateAgedOver5y(40, 100, Defaults)!;              // critical
        var noDate = StockInsightRuleEngine.EvaluateNoProductionDate(3)!;                      // warning

        var problems = new[] { noCost, aged, noDate };
        var actions = StockInsightRuleEngine.BuildActions(problems, new List<jewelry.Model.Production.Insight.Wip.Finding>());

        Assert.Equal(new[] { "ACT_CLEAR_AGED", "ACT_PRICE_NO_COST", "ACT_FIX_PRODUCTION_DATE" }, actions.Select(a => a.Code).ToArray());
        Assert.Equal(new[] { 1, 2, 3 }, actions.Select(a => a.Priority).ToArray());
        Assert.Equal("critical", StockInsightRuleEngine.WorstSeverity(problems));
        Assert.Equal("ok", StockInsightRuleEngine.WorstSeverity(new List<jewelry.Model.Production.Insight.Wip.Finding>()));
    }
}
