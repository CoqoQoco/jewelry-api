using Jewelry.Service.Report.Executive;
using Finding = jewelry.Model.Production.Insight.Wip.Finding;

namespace Jewelry.Service.Tests.Report.Executive;

public class ExecutiveDigestRankerTests
{
    private static DigestTabFinding F(string code, string severity, string tab = "x")
        => new(new Finding { Code = code, Severity = severity, ReportRef = "ref-" + code }, tab);

    private static DigestDomainInput D(string key, bool available = true, int forecasts = 0, params DigestTabFinding[] problems)
        => new() { Key = key, Available = available, ForecastCount = forecasts, Problems = problems.ToList() };

    private static List<DigestDomainInput> Empty() => new()
    {
        D("production"), D("sales"), D("material"), D("stock")
    };

    [Fact]
    public void Empty_AllOk_NoTop()
    {
        var r = ExecutiveDigestRanker.Build(DateTime.UtcNow, "ALL", Empty());
        Assert.Equal(new[] { "production", "sales", "material", "stock" }, r.Domains.Select(d => d.Key));
        Assert.All(r.Domains, d =>
        {
            Assert.Equal("ok", d.Status);
            Assert.True(d.Available);
            Assert.Equal(0, d.ProblemCount);
        });
        Assert.Empty(r.Top);
    }

    [Fact]
    public void Status_IsWorstSeverity_AndCountsProblemsAndForecasts()
    {
        var inputs = new List<DigestDomainInput>
        {
            D("production", true, 3, F("A", "warning"), F("B", "critical"), F("C", "info")),
            D("sales", true, 0, F("D", "warning")),
            D("material", true, 1, F("E", "info")),
            D("stock")
        };
        var domains = ExecutiveDigestRanker.BuildDomains(inputs);
        Assert.Equal("critical", domains[0].Status);
        Assert.Equal(3, domains[0].ProblemCount);
        Assert.Equal(3, domains[0].ForecastCount);
        Assert.Equal("warning", domains[1].Status);
        Assert.Equal("ok", domains[2].Status);
        Assert.Equal("ok", domains[3].Status);
    }

    [Fact]
    public void Top_CriticalBeforeWarningBeforeInfo_StableByDomainOrder()
    {
        var inputs = new List<DigestDomainInput>
        {
            D("production", true, 0, F("P-info", "info"), F("P-warn", "warning")),
            D("sales", true, 0, F("S-warn", "warning")),
            D("material", true, 0, F("M-crit", "critical")),
            D("stock", true, 0, F("K-crit", "critical"))
        };
        var top = ExecutiveDigestRanker.BuildTop(inputs);
        Assert.Equal(new[] { "M-crit", "K-crit", "P-warn", "S-warn", "P-info" }, top.Select(t => t.Code));
    }

    [Fact]
    public void Top_CapsTwoPerDomain_AndFiveTotal()
    {
        var inputs = new List<DigestDomainInput>
        {
            D("production", true, 0, F("P1", "critical"), F("P2", "critical"), F("P3", "critical")),
            D("sales", true, 0, F("S1", "critical"), F("S2", "critical"), F("S3", "critical")),
            D("material", true, 0, F("M1", "warning"), F("M2", "warning")),
            D("stock", true, 0, F("K1", "warning"))
        };
        var top = ExecutiveDigestRanker.BuildTop(inputs);
        Assert.Equal(new[] { "P1", "P2", "S1", "S2", "M1" }, top.Select(t => t.Code));
        Assert.Equal(5, top.Count);
    }

    [Fact]
    public void Top_KeepsSourceFindingFieldsAndTab()
    {
        var finding = new Finding { Code = "WIP_X", Severity = "warning", ReportRef = "dueRisk", Params = new() { ["count"] = 7 } };
        var inputs = new List<DigestDomainInput>
        {
            D("production", true, 0, new DigestTabFinding(finding, "delivery"))
        };
        var item = Assert.Single(ExecutiveDigestRanker.BuildTop(inputs));
        Assert.Equal("production", item.Domain);
        Assert.Equal("delivery", item.Tab);
        Assert.Equal("WIP_X", item.Code);
        Assert.Equal("dueRisk", item.ReportRef);
        Assert.Equal(7, item.Params["count"]);
        Assert.Equal("warning", item.Severity);
    }

    [Fact]
    public void FailedDomain_IsUnknownAndExcluded_OthersPresent()
    {
        var inputs = new List<DigestDomainInput>
        {
            D("production", false, 0, F("P1", "critical")),
            D("sales", true, 2, F("S1", "warning")),
            D("material"),
            D("stock")
        };
        var r = ExecutiveDigestRanker.Build(DateTime.UtcNow, "GOLD", inputs);
        Assert.Equal("GOLD", r.Metal);
        Assert.Equal("unknown", r.Domains[0].Status);
        Assert.False(r.Domains[0].Available);
        Assert.Equal(0, r.Domains[0].ProblemCount);
        Assert.Equal("warning", r.Domains[1].Status);
        Assert.Equal(2, r.Domains[1].ForecastCount);
        Assert.Equal("S1", Assert.Single(r.Top).Code);
    }

    [Fact]
    public void MissingDomain_TreatedAsUnavailable()
    {
        var domains = ExecutiveDigestRanker.BuildDomains(new[] { D("sales") });
        Assert.Equal(4, domains.Count);
        Assert.Equal("unknown", domains[0].Status);
        Assert.Equal("ok", domains[1].Status);
    }

    [Fact]
    public void Merge_SumsCountsAndTagsTopicTab()
    {
        var input = ExecutiveDigestService.Merge("production", new[]
        {
            ("wip", new List<Finding> { new() { Code = "A", Severity = "warning", ReportRef = "r" } }, new List<Finding> { new() { Code = "F", Severity = "info", ReportRef = "r" } }),
            ("gold", new List<Finding> { new() { Code = "B", Severity = "critical", ReportRef = "r" } }, new List<Finding>())
        });
        Assert.True(input.Available);
        Assert.Equal(2, input.Problems.Count);
        Assert.Equal(1, input.ForecastCount);
        Assert.Equal("gold", input.Problems[1].Tab);
        Assert.Equal("critical", ExecutiveDigestRanker.WorstStatus(input.Problems));
    }

    [Fact]
    public void CacheKey_FallsUnderMaterialInvalidationPrefix()
    {
        Assert.StartsWith(Jewelry.Service.Material.Insight.MaterialInsightService.CachePrefix("GOLD"), ExecutiveDigestService.CacheKey("ALL"));
    }
}
