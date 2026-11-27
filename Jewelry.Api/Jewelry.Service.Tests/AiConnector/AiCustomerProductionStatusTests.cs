using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.Common;
using Jewelry.Service.AiConnector.Pipeline;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Tools.Production;
using Jewelry.Service.Production.Insight;
using Jewelry.Service.Production.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Security.Claims;
using CustomerStatus = jewelry.Model.Production.Insight.CustomerStatus;

namespace Jewelry.Service.Tests.AiConnector;

public class AiCustomerProductionStatusTests
{
    private class FakeLookup : IAiCustomerLookup
    {
        public Dictionary<string, string> Codes { get; } = new(StringComparer.OrdinalIgnoreCase) { ["AL01"] = "AL01" };
        public List<string> NameMatches { get; set; } = new() { "AL01" };
        public List<string> Patterns { get; } = new();
        public int CodeCalls;

        public Task<List<string>> FindByCode(string code, CancellationToken ct)
        {
            CodeCalls++;
            return Task.FromResult(Codes.TryGetValue(code, out var c) ? new List<string> { c } : new List<string>());
        }

        public Task<List<string>> FindByNamePattern(string pattern, int max, CancellationToken ct)
        {
            Patterns.Add(pattern);
            return Task.FromResult(NameMatches.Take(max).ToList());
        }
    }

    public class InsightProxy : DispatchProxy
    {
        public List<List<string>> Requests { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name != nameof(IProductionInsightService.CustomerStatus))
                throw new NotSupportedException(targetMethod.Name);
            var req = (CustomerStatus.Request)args![0]!;
            Requests.Add(req.CustomerCodes);
            var response = new CustomerStatus.Response
            {
                AsOf = DateTime.UtcNow,
                Customers = req.CustomerCodes.Select(code => new CustomerStatus.CustomerItem
                {
                    CustomerCode = code,
                    CustomerName = "Al Sulaiman Jewellery",
                    OpenPlanCount = 2,
                    MeltedCount = 1,
                    StageCounts = { new CustomerStatus.StageCount { Status = 50, StatusName = "ช่างแต่ง", Count = 2 } },
                    Plans =
                    {
                        new CustomerStatus.PlanItem { Wo = "W1", WoNumber = 1, ProductNumber = "P1", Mold = "M1", Status = 50, StatusName = "ช่างแต่ง", LastMoveDate = DateTime.UtcNow.AddDays(-9), DaysSinceMove = 9 },
                    },
                }).ToList(),
            };
            return Task.FromResult(response);
        }
    }

    private static (IServiceProvider Services, FakeLookup Lookup, InsightProxy Insight) Build()
    {
        var lookup = new FakeLookup();
        var insight = DispatchProxy.Create<IProductionInsightService, InsightProxy>();
        var services = new ServiceCollection()
            .AddSingleton<IAiCustomerLookup>(lookup)
            .AddSingleton(insight)
            .BuildServiceProvider();
        return (services, lookup, (InsightProxy)(object)insight);
    }

    private static AiToolContext Ctx(IServiceProvider services, bool dataScope, bool executiveView)
    {
        var scopes = new HashSet<string> { AiScopes.ModuleProduction };
        if (dataScope) scopes.Add(AiScopes.DataCustomerNames);
        var perms = new HashSet<string> { PermissionCodes.ProductionView };
        if (executiveView) perms.Add(PermissionCodes.ExecutiveView);
        return new AiToolContext { UserId = 1, Services = services, Scopes = scopes, Permissions = perms };
    }

    private static Task<AiCustomerStatusOutput> Run(AiToolContext ctx, string? customer) =>
        (Task<AiCustomerStatusOutput>)typeof(CustomerProductionStatusTool)
            .GetMethod("RunAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(new CustomerProductionStatusTool(), new object[] { new AiCustomerStatusInput { Customer = customer }, ctx, CancellationToken.None })!;

    [Fact]
    public async Task CodeSearch_WorksWithoutNameAllowance_NeverUsesNameSearch()
    {
        var (s, lookup, insight) = Build();
        var r = await Run(Ctx(s, dataScope: false, executiveView: false), "al01");
        Assert.Single(r.Customers);
        Assert.Equal("AL01", r.Customers[0].CustomerCode);
        Assert.Empty(lookup.Patterns);
        Assert.Equal(new[] { "AL01" }, insight.Requests.Single());
        Assert.Null(r.Message);
    }

    [Fact]
    public async Task NameSearch_Allowed_UsesEscapedPatternAndResolvesCodes()
    {
        var (s, lookup, insight) = Build();
        lookup.NameMatches = new() { "AL01", "AL02" };
        var r = await Run(Ctx(s, true, true), "Al Sulaiman");
        Assert.Equal(2, r.Customers.Count);
        Assert.Equal("%Al Sulaiman%", lookup.Patterns.Single());
        Assert.Equal(new[] { "AL01", "AL02" }, insight.Requests.Single());
    }

    [Fact]
    public async Task NameSearch_WithoutDataScope_UniformError()
    {
        var (s, lookup, insight) = Build();
        var r = await Run(Ctx(s, dataScope: false, executiveView: true), "Al Sulaiman");
        Assert.Empty(r.Customers);
        Assert.Equal(CustomerProductionStatusTool.NotFoundMessage, r.Message);
        Assert.Empty(lookup.Patterns);
        Assert.Empty(insight.Requests);
    }

    [Fact]
    public async Task NameSearch_WithoutExecutiveView_UniformError()
    {
        var (s, lookup, _) = Build();
        var r = await Run(Ctx(s, dataScope: true, executiveView: false), "Al Sulaiman");
        Assert.Empty(r.Customers);
        Assert.Equal(CustomerProductionStatusTool.NotFoundMessage, r.Message);
        Assert.Empty(lookup.Patterns);
    }

    [Theory]
    [InlineData("100%", "%100\\%%")]
    [InlineData("a_b", "%a\\_b%")]
    [InlineData("a\\b", "%a\\\\b%")]
    [InlineData("%_\\", "%\\%\\_\\\\%")]
    public async Task NameSearch_EscapesWildcards(string query, string expectedPattern)
    {
        Assert.Equal(expectedPattern, CustomerProductionStatusTool.BuildNamePattern(query));
        var (s, lookup, _) = Build();
        lookup.NameMatches = new();
        await Run(Ctx(s, true, true), query.Length >= 3 ? query : query + "xx");
        if (query.Length >= 3) Assert.Equal(expectedPattern, lookup.Patterns.Single());
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("  ab  ")]
    [InlineData("%%")]
    public async Task NameSearch_UnderThreeChars_RejectedWithoutQuery(string query)
    {
        var (s, lookup, insight) = Build();
        var r = await Run(Ctx(s, true, true), query);
        Assert.Empty(lookup.Patterns);
        Assert.Empty(insight.Requests);
        Assert.Equal(CustomerProductionStatusTool.NotFoundMessage, r.Message);
    }

    [Fact]
    public async Task AllFailureCases_ShareSameMessage()
    {
        var (s, lookup, _) = Build();
        lookup.NameMatches = new();
        var messages = new[]
        {
            (await Run(Ctx(s, false, false), "Al Sulaiman")).Message,
            (await Run(Ctx(s, true, false), "Al Sulaiman")).Message,
            (await Run(Ctx(s, true, true), "ไม่มีลูกค้านี้")).Message,
            (await Run(Ctx(s, true, true), "ab")).Message,
            (await Run(Ctx(s, true, true), "")).Message,
            (await Run(Ctx(s, false, false), null)).Message,
        };
        Assert.All(messages, m => Assert.Equal(CustomerProductionStatusTool.NotFoundMessage, m));
    }

    [Fact]
    public async Task NameSearchResult_DoesNotLeakToCallerWithoutAllowance_ViaPipeline()
    {
        var env = new AiTestEnv();
        var (services, lookup, _) = Build();
        var tool = new CustomerProductionStatusTool();
        Assert.Equal(TimeSpan.Zero, tool.CacheTtl);
        var registry = new AiToolRegistry(new IAiTool[] { tool });
        var cache = new InsightCache(new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions())));
        var pipeline = new AiToolPipeline(registry, env.Users, cache, new AiGlossaryService(env.Context),
            new AiRateLimiter(100, () => DateTime.UtcNow), env.Context, NullLogger<AiToolPipeline>.Instance, services);

        ClaimsPrincipal Caller(params string[] scopes)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1"), new(ClaimTypes.Name, "boss"), new(AiPrincipalFactory.TokenIdClaim, "5") };
            claims.AddRange(scopes.Select(s => new Claim(AiPrincipalFactory.ScopeClaim, s)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "t"));
        }

        const string args = "{\"customer\":\"Al Sulaiman\"}";
        var a = await pipeline.Run(Caller(AiScopes.ModuleProduction, AiScopes.DataCustomerNames), tool.Name, args, default);
        Assert.False(a.IsError);
        Assert.Contains("Al Sulaiman Jewellery", a.Json);
        Assert.Single(lookup.Patterns);

        var b = await pipeline.Run(Caller(AiScopes.ModuleProduction), tool.Name, args, default);
        Assert.False(b.IsError);
        Assert.DoesNotContain("Sulaiman", b.Json);
        var data = JObject.Parse(b.Json)["data"]!;
        Assert.Empty((JArray)data["customers"]!);
        Assert.Equal(CustomerProductionStatusTool.NotFoundMessage, (string?)data["message"]);
        Assert.Single(lookup.Patterns);

        // ค้นด้วยรหัสโดยไม่มี scope ชื่อ: ได้ข้อมูล แต่ชื่อถูก mask
        var c = await pipeline.Run(Caller(AiScopes.ModuleProduction), tool.Name, "{\"customer\":\"AL01\"}", default);
        Assert.False(c.IsError);
        Assert.DoesNotContain("Sulaiman", c.Json);
        Assert.Contains("\"openPlanCount\":2", c.Json);
    }

    [Fact]
    public async Task MissingModuleProductionScope_Denied()
    {
        var env = new AiTestEnv();
        var (services, _, _) = Build();
        var tool = new CustomerProductionStatusTool();
        var pipeline = new AiToolPipeline(new AiToolRegistry(new IAiTool[] { tool }), env.Users,
            new InsightCache(new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions()))),
            new AiGlossaryService(env.Context), new AiRateLimiter(100, () => DateTime.UtcNow), env.Context, NullLogger<AiToolPipeline>.Instance, services);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(AiPrincipalFactory.TokenIdClaim, "5"), new Claim(AiPrincipalFactory.ScopeClaim, AiScopes.ModuleExecutive) };
        var r = await pipeline.Run(new ClaimsPrincipal(new ClaimsIdentity(claims, "t")), tool.Name, "{\"customer\":\"AL01\"}", default);
        Assert.Equal(AiErrorCodes.ScopeDenied, r.ErrorCode);
    }

    // ---- นิยาม "ค้าง" ต้องตรงกับ ProductionPlanWipHelper (แท็บ WIP) ----

    private static int _planSeq;

    private static TbtProductionPlan Plan(string customer, int status, string wo, bool active = true, string? product = null) => new()
    {
        Id = ++_planSeq,
        Wo = wo,
        WoNumber = _planSeq,
        CustomerNumber = customer,
        Status = status,
        IsActive = active,
        Mold = "M",
        ProductNumber = product ?? "P" + _planSeq,
        ProductName = "name",
        ProductDetail = "",
        ProductType = "",
        ProductQtyUnit = "",
        WoText = "",
        CustomerType = "",
        CreateBy = "t",
        CreateDate = DateTime.UtcNow.AddDays(-30),
        RequestDate = DateTime.UtcNow,
    };

    [Fact]
    public async Task ServiceCounts_MatchWipDefinition_AndExcludeMeltedCvdBackdateTest()
    {
        var env = new AiTestEnv();
        var ctx = env.Context;
        var plans = new[]
        {
            Plan("AL01", 50, "OPEN-OLD"),
            Plan("AL01", 50, "OPEN-NEW"),
            Plan("AL01", 80, "OPEN-SET"),
            Plan("AL01", 100, "DONE"),
            Plan("AL01", 500, "MELT1"),
            Plan("AL01", 500, "MELT2"),
            Plan("AL01", 84, "WAITCVD"),
            Plan("AL01", 85, "CVD"),
            Plan("AL01", 50, "INACTIVE", active: false),
            Plan("AL01", 50, "TEST-1"),
            Plan("AL01", 50, "BACKDATE"),
            Plan("AL02", 50, "OTHER"),
        };
        ctx.TbtProductionPlan.AddRange(plans);
        ctx.TbmCustomer.Add(new TbmCustomer { Code = "AL01", NameTh = "อัล สุไลมาน", TypeCode = "x", CreateBy = "t" });
        ctx.TbmProductionPlanStatus.AddRange(
            new TbmProductionPlanStatus { Id = 50, NameEn = "Trim", NameTh = "ช่างแต่ง" },
            new TbmProductionPlanStatus { Id = 80, NameEn = "Set", NameTh = "ช่างฝัง" });
        var now = DateTime.UtcNow;
        ctx.TbtProductionPlanStatusHeader.AddRange(
            new TbtProductionPlanStatusHeader { ProductionPlanId = plans[0].Id, Status = 50, CreateBy = "t", CreateDate = now.AddDays(-20), IsActive = true },
            new TbtProductionPlanStatusHeader { ProductionPlanId = plans[0].Id, Status = 50, CreateBy = "t", CreateDate = now.AddDays(-10), IsActive = true },
            new TbtProductionPlanStatusHeader { ProductionPlanId = plans[1].Id, Status = 50, CreateBy = "t", CreateDate = now.AddDays(-2), IsActive = true },
            new TbtProductionPlanStatusHeader { ProductionPlanId = plans[10].Id, Status = 100, CreateBy = "t", CreateDate = now.AddDays(-1), IsActive = true, Remark1 = jewelry.Model.Constant.BackdateCloseMarker.RemarkPrefix + " x" });
        await ctx.SaveChangesAsync();

        var helper = new ProductionPlanWipHelper(ctx, new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions())), null!);
        var service = new ProductionInsightService(ctx, null!, helper, null!, null!, null!, null!, null!, null!, null!, null!);

        var r = await service.CustomerStatus(new CustomerStatus.Request { CustomerCodes = new List<string> { "AL01" } });
        var c = Assert.Single(r.Customers);
        Assert.Equal("อัล สุไลมาน", c.CustomerName);
        Assert.Equal(3, c.OpenPlanCount);
        Assert.Equal(2, c.MeltedCount);
        Assert.Equal(c.OpenPlanCount, c.StageCounts.Sum(s => s.Count));
        Assert.Equal(2, c.StageCounts.Single(s => s.Status == 50).Count);
        Assert.Equal(new[] { "OPEN-SET", "OPEN-OLD", "OPEN-NEW" }.OrderBy(x => x).ToArray(), c.Plans.Select(p => p.Wo).OrderBy(x => x).ToArray());
        Assert.Equal("OPEN-OLD", c.Plans.Single(p => p.Status == 50 && p.DaysSinceMove >= 10).Wo);
        Assert.True(c.Plans.Zip(c.Plans.Skip(1), (a, b) => a.LastMoveDate <= b.LastMoveDate).All(x => x));
        Assert.False(r.Truncated);

        // ตัวเลขต้องไม่เกินที่ WIP helper นับ (WIP นับ WaitCVD/CVD ด้วย จึงมากกว่าเท่ากับ — ส่วนต่างต้องเป็น WaitCVD+CVD+ใบทดสอบ+ปิดย้อนหลัง เท่านั้น)
        var wipOpen = (await helper.GetOpenPlansAsync()).Count(p => plans.Where(x => x.CustomerNumber == "AL01").Select(x => x.Id).Contains(p.Id));
        Assert.Equal(c.OpenPlanCount + 2 /*84,85*/ + 1 /*TEST*/ + 1 /*ปิดย้อนหลัง ที่ยัง status 50 ในข้อมูลทดสอบ*/, wipOpen);
    }

    [Fact]
    public async Task ServiceCaps_TotalPlansAt200_WithTruncatedFlag()
    {
        var env = new AiTestEnv();
        var ctx = env.Context;
        ctx.TbtProductionPlan.AddRange(Enumerable.Range(0, 230).Select(i => Plan("AL09", 50, "W" + i)));
        await ctx.SaveChangesAsync();
        var helper = new ProductionPlanWipHelper(ctx, new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions())), null!);
        var service = new ProductionInsightService(ctx, null!, helper, null!, null!, null!, null!, null!, null!, null!, null!);
        var r = await service.CustomerStatus(new CustomerStatus.Request { CustomerCodes = new List<string> { "AL09" } });
        var c = Assert.Single(r.Customers);
        Assert.Equal(230, c.OpenPlanCount);
        Assert.Equal(200, c.Plans.Count);
        Assert.True(c.Truncated);
        Assert.True(r.Truncated);
    }

    [Fact]
    public async Task ServiceGroupsFreeTextCustomerNumber_CaseInsensitive_NonMasterBecomesNameWithNullCode()
    {
        var env = new AiTestEnv();
        var ctx = env.Context;
        ctx.TbmCustomer.Add(new TbmCustomer { Code = "EX004", NameTh = "Al Sulaiman Jewellery", TypeCode = "x", CreateBy = "t" });
        ctx.TbtProductionPlan.AddRange(
            Plan("EX004", 50, "A1"),
            Plan(" ex004 ", 80, "A2"),
            Plan("AL SULAIMAN", 50, "B1"),
            Plan("al sulaiman ", 50, "B2"),
            Plan("AL SULAIMAN", 500, "B3"),
            Plan("OTHER", 50, "C1"));
        await ctx.SaveChangesAsync();
        var helper = new ProductionPlanWipHelper(ctx, new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions())), null!);
        var service = new ProductionInsightService(ctx, null!, helper, null!, null!, null!, null!, null!, null!, null!, null!);

        var r = await service.CustomerStatus(new CustomerStatus.Request { CustomerCodes = new List<string> { "ex004", "AL SULAIMAN", "Al Sulaiman " } });
        Assert.Equal(2, r.Customers.Count);
        var master = r.Customers.Single(c => c.CustomerCode != null);
        Assert.Equal("EX004", master.CustomerCode);
        Assert.Equal("Al Sulaiman Jewellery", master.CustomerName);
        Assert.Equal(2, master.OpenPlanCount);
        var raw = r.Customers.Single(c => c.CustomerCode == null);
        Assert.Equal("AL SULAIMAN", raw.CustomerName);
        Assert.Equal(2, raw.OpenPlanCount);
        Assert.Equal(1, raw.MeltedCount);
    }

    [Fact]
    public async Task NonMasterCustomer_CodeSearchByNonAllowedCaller_NameMasked_CodeNull()
    {
        var env = new AiTestEnv();
        var (services, lookup, _) = Build();
        lookup.Codes["AL SULAIMAN"] = "AL SULAIMAN";
        var tool = new CustomerProductionStatusTool();
        var pipeline = new AiToolPipeline(new AiToolRegistry(new IAiTool[] { tool }), env.Users,
            new InsightCache(new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions()))),
            new AiGlossaryService(env.Context), new AiRateLimiter(100, () => DateTime.UtcNow), env.Context, NullLogger<AiToolPipeline>.Instance, services);
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(AiPrincipalFactory.TokenIdClaim, "5"), new Claim(AiPrincipalFactory.ScopeClaim, AiScopes.ModuleProduction) };
        var r = await pipeline.Run(new ClaimsPrincipal(new ClaimsIdentity(claims, "t")), tool.Name, "{\"customer\":\"al sulaiman\"}", default);
        Assert.False(r.IsError);
        Assert.DoesNotContain("Sulaiman", r.Json);
    }

    [Fact]
    public void LookupMerge_DedupesCaseAndWhitespace_MasterFirst_AndCapsAtMax()
    {
        var merged = AiCustomerLookup.Merge(new[] { "EX004" }, new[] { " ex004", "AL SULAIMAN", "al sulaiman ", "X" }, 10);
        Assert.Equal(new[] { "EX004", "AL SULAIMAN", "X" }, merged);
        Assert.Equal(2, AiCustomerLookup.Merge(new[] { "A" }, new[] { "B", "C" }, 2).Count);
    }
}
