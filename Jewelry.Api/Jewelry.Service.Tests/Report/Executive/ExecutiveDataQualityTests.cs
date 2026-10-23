using jewelry.Model.Report.Executive.DataQuality;
using Jewelry.Service.Common;
using Jewelry.Service.Report.Executive;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using C = Jewelry.Service.Report.Executive.ExecutiveDataQualityCalculator;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveDataQualityTests
{
    private static readonly ThresholdValues T = ExecutiveDataQualityThresholds.Resolve(null);

    private static DataQualityInput In(string code, int count, int total, decimal? amount = null)
        => new() { Code = code, Count = count, Total = total, AmountThb = amount };

    private static List<Check> Build(params DataQualityInput[] inputs) => C.BuildChecks(inputs, T);

    private static Check Get(List<Check> checks, string code) => checks.Single(c => c.Code == code);

    // ---- thresholds ----

    [Fact]
    public void Resolve_Defaults_And_Clamping()
    {
        Assert.Equal(10m, T.WarnPercent);
        Assert.Equal(50m, T.CriticalPercent);

        var r = ExecutiveDataQualityThresholds.Resolve(new ThresholdOverrides { WarnPercent = 80m, CriticalPercent = 20m });
        Assert.Equal(80m, r.WarnPercent);
        Assert.Equal(80m, r.CriticalPercent);

        var clamped = ExecutiveDataQualityThresholds.Resolve(new ThresholdOverrides { WarnPercent = -5m, CriticalPercent = 400m });
        Assert.Equal(0m, clamped.WarnPercent);
        Assert.Equal(100m, clamped.CriticalPercent);
    }

    // ---- severity ----

    [Theory]
    [InlineData(0, 100, "ok")]
    [InlineData(5, 100, "ok")]
    [InlineData(10, 100, "ok")]
    [InlineData(11, 100, "warning")]
    [InlineData(50, 100, "warning")]
    [InlineData(51, 100, "critical")]
    [InlineData(100, 100, "critical")]
    public void Ratio_Boundaries_AreStrictlyGreaterThan(int count, int total, string expected)
    {
        var check = Get(Build(In(C.InvoiceNoDue, count, total)), C.InvoiceNoDue);
        Assert.Equal(expected, check.Severity);
    }

    [Fact]
    public void Ratio_UsesUnroundedValue()
    {
        // 10.004% ปัด 2 ตำแหน่งได้ 10.00 แต่ต้องเกิน 10 จริง -> warning
        var check = Get(Build(In(C.InvoiceNoDue, 100040, 1000000)), C.InvoiceNoDue);
        Assert.Equal(10m, check.Ratio);
        Assert.Equal("warning", check.Severity);
    }

    [Fact]
    public void NoCost_CappedAtWarning_EvenAt90Percent()
    {
        var check = Get(Build(In(C.NoCost, 90, 100)), C.NoCost);
        Assert.Equal("warning", check.Severity);
        Assert.Equal(90m, check.Ratio);
        Assert.Equal(true, check.Params["cappedAtWarning"]);
    }

    [Fact]
    public void NoCost_BelowThreshold_IsOk()
    {
        Assert.Equal("ok", Get(Build(In(C.NoCost, 10, 100)), C.NoCost).Severity);
        Assert.Equal("warning", Get(Build(In(C.NoCost, 11, 100)), C.NoCost).Severity);
    }

    [Fact]
    public void ZeroCount_IsOk_EvenWithZeroTotal()
    {
        var checks = Build(In(C.NoCost, 0, 0), In(C.NegativeGold, 0, 0), In(C.SoNoInvoice, 0, 0));
        Assert.All(checks.Where(c => c.Available), c => Assert.Equal("ok", c.Severity));
        Assert.Equal(0m, Get(checks, C.NoCost).Ratio);
    }

    [Fact]
    public void CountOnly_Checks_WarnOnAnyCount_NeverCritical()
    {
        var checks = Build(In(C.NegativeGold, 1, 1000), In(C.SoNoInvoice, 40, 40, 1234.567m));
        Assert.Equal("warning", Get(checks, C.NegativeGold).Severity);
        var so = Get(checks, C.SoNoInvoice);
        Assert.Equal("warning", so.Severity);
        Assert.Equal(100m, so.Ratio);
        Assert.Equal(1234.57m, so.Params["amountThb"]);
    }

    [Fact]
    public void CustomThresholds_ChangeBoundaries()
    {
        var t = ExecutiveDataQualityThresholds.Resolve(new ThresholdOverrides { WarnPercent = 20m, CriticalPercent = 30m });
        var checks = C.BuildChecks(new[] { In(C.InvoiceNoDue, 25, 100), In(C.SoNoDeliveryDate, 31, 100) }, t);
        Assert.Equal("warning", Get(checks, C.InvoiceNoDue).Severity);
        Assert.Equal("critical", Get(checks, C.SoNoDeliveryDate).Severity);
    }

    // ---- checks / availability / kpi / status ----

    [Fact]
    public void BuildChecks_AlwaysReturnsAllSix_InDefinitionOrder_MissingIsUnavailable()
    {
        var checks = Build(In(C.NoCost, 1, 10));
        Assert.Equal(
            new[] { C.NoCost, C.NegativeGold, C.InvoiceNoDue, C.SoNoDeliveryDate, C.SoNoInvoice, C.PlaceholderWorkerWip },
            checks.Select(c => c.Code));
        Assert.True(Get(checks, C.NoCost).Available);
        Assert.All(checks.Where(c => c.Code != C.NoCost), c => Assert.False(c.Available));
    }

    [Fact]
    public void Kpi_ExcludesUnavailable_AndCountsPassedWarningCritical()
    {
        var checks = Build(
            In(C.NoCost, 0, 100),
            In(C.NegativeGold, 2, 20),
            In(C.InvoiceNoDue, 80, 100),
            In(C.SoNoDeliveryDate, 1, 100));
        var kpi = C.BuildKpi(checks);

        Assert.Equal(4, kpi.Total);
        Assert.Equal(1, kpi.CriticalCount);
        Assert.Equal(1, kpi.WarningCount);
        Assert.Equal(2, kpi.PassedCount);
    }

    [Fact]
    public void Kpi_AffectedTabs_AreDistinct_AndOnlyFailing()
    {
        var checks = Build(
            In(C.NoCost, 0, 100),
            In(C.NegativeGold, 2, 20),
            In(C.InvoiceNoDue, 80, 100),
            In(C.SoNoDeliveryDate, 90, 100),
            In(C.SoNoInvoice, 3, 9));
        Assert.Equal(new[] { "material", "sales" }, C.BuildKpi(checks).AffectedTabs);
    }

    [Fact]
    public void Status_IsWorstSeverity_IgnoringUnavailable()
    {
        Assert.Equal("ok", C.WorstSeverity(Build(In(C.NoCost, 0, 10))));
        Assert.Equal("warning", C.WorstSeverity(Build(In(C.NoCost, 9, 10), In(C.NegativeGold, 0, 3))));
        Assert.Equal("critical", C.WorstSeverity(Build(In(C.NoCost, 9, 10), In(C.InvoiceNoDue, 9, 10))));
        Assert.Equal("ok", C.WorstSeverity(Build()));
    }

    [Fact]
    public void Check_Params_CarryCountTotalRatioAndExtras()
    {
        var input = In(C.NegativeGold, 3, 12);
        input.Params["negativeWeight"] = -5.5m;
        var check = Get(Build(input), C.NegativeGold);

        Assert.Equal(3, check.Params["count"]);
        Assert.Equal(12, check.Params["total"]);
        Assert.Equal(25m, check.Params["ratio"]);
        Assert.Equal(-5.5m, check.Params["negativeWeight"]);
        Assert.Equal("material", check.Tab);
        Assert.Equal("matItems", check.ReportRef);
    }

    // ---- problems ----

    [Fact]
    public void Problems_OnlyFailingAvailableChecks_WithStableCodes()
    {
        var checks = Build(
            In(C.NoCost, 50, 100),
            In(C.NegativeGold, 0, 10),
            In(C.InvoiceNoDue, 5, 100),
            In(C.SoNoDeliveryDate, 60, 100),
            In(C.SoNoInvoice, 4, 8, 99m));
        var problems = ExecutiveDataQualityRuleEngine.BuildProblems(checks);

        Assert.Equal(new[] { "DQ_NO_COST", "DQ_SO_NO_DELIVERY", "DQ_SO_NO_INVOICE" }, problems.Select(p => p.Code));
        Assert.Equal(new[] { "warning", "critical", "warning" }, problems.Select(p => p.Severity));
        Assert.Equal(new[] { "stkCost", "salSo", "salSo" }, problems.Select(p => p.ReportRef));
        Assert.Equal(60, problems[1].Params["count"]);
        Assert.Equal(100, problems[1].Params["total"]);
        Assert.Equal(60m, problems[1].Params["ratio"]);
        Assert.Equal(99m, problems[2].Params["amountThb"]);
    }

    [Fact]
    public void Problems_Empty_WhenAllPass()
    {
        Assert.Empty(ExecutiveDataQualityRuleEngine.BuildProblems(Build(In(C.NoCost, 0, 10), In(C.InvoiceNoDue, 1, 100))));
    }

    // ---- actions ----

    [Fact]
    public void Actions_OrderedByWorstSeverity_ThenDeclaration_WithRelatedCodesAndOwner()
    {
        var checks = Build(
            In(C.NoCost, 90, 100),
            In(C.NegativeGold, 2, 20),
            In(C.InvoiceNoDue, 80, 100),
            In(C.SoNoInvoice, 3, 9, 500m));
        var actions = ExecutiveDataQualityRuleEngine.BuildActions(ExecutiveDataQualityRuleEngine.BuildProblems(checks));

        Assert.Equal(
            new[] { "ACT_DQ_SET_INVOICE_DUE", "ACT_DQ_PRICE_NO_COST", "ACT_DQ_RECORD_OPENING_BALANCE", "ACT_DQ_INVOICE_FROM_SO" },
            actions.Select(a => a.Code));
        Assert.Equal(new[] { 1, 2, 3, 4 }, actions.Select(a => a.Priority));

        var price = actions.Single(a => a.Code == "ACT_DQ_PRICE_NO_COST");
        Assert.Equal("productionManager", price.OwnerRole);
        Assert.Equal(new[] { "DQ_NO_COST" }, price.RelatedCodes);
        Assert.Equal(90, price.Params["count"]);

        Assert.Equal("deptHead", actions.Single(a => a.Code == "ACT_DQ_RECORD_OPENING_BALANCE").OwnerRole);
        var invoice = actions.Single(a => a.Code == "ACT_DQ_INVOICE_FROM_SO");
        Assert.Equal("saleManager", invoice.OwnerRole);
        Assert.Equal(500m, invoice.Params["amountThb"]);
    }

    [Fact]
    public void Actions_Empty_WhenNoProblems()
    {
        Assert.Empty(ExecutiveDataQualityRuleEngine.BuildActions(new List<jewelry.Model.Production.Insight.Wip.Finding>()));
    }

    // ---- cache ----

    [Fact]
    public void CacheKey_IsPerMetal()
    {
        Assert.Equal("DataQuality:Overview:ALL", ExecutiveDataQualityService.CacheKey("ALL"));
        Assert.Equal("DataQuality:Overview:GOLD", ExecutiveDataQualityService.CacheKey("GOLD"));
    }

    private static InsightCache NewCache() => new(new MemoryCache(Options.Create(new MemoryCacheOptions())));

    [Fact]
    public async Task Cache_HitsWithoutOverride_SkipsWithOverride()
    {
        var cache = NewCache();
        var key = ExecutiveDataQualityService.CacheKey("ALL");
        var calls = 0;
        Task<Response> Factory() { calls++; return Task.FromResult(new Response()); }

        await cache.GetOrCreateAsync(key, InsightCache.OverviewTtl, (ThresholdOverrides?)null, Factory);
        await cache.GetOrCreateAsync(key, InsightCache.OverviewTtl, (ThresholdOverrides?)null, Factory);
        Assert.Equal(1, calls);

        var over = new ThresholdOverrides { WarnPercent = 5m };
        await cache.GetOrCreateAsync(key, InsightCache.OverviewTtl, over, Factory);
        await cache.GetOrCreateAsync(key, InsightCache.OverviewTtl, over, Factory);
        Assert.Equal(3, calls);
    }
}
