using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.AiConnector.Pipeline;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace Jewelry.Service.Tests.AiConnector;

public class AiPipelineTests
{
    public class CostOutput
    {
        [AiNotSensitive] public int Count { get; set; }
        [AiSensitive(SensitiveCategory.Cost)] public decimal? CostThb { get; set; }
        [AiNotSensitive] public List<AiMetric> Rows { get; set; } = new();
    }

    public class RangedInput : IAiRangedInput, IAiRowCappedInput
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? Limit { get; set; }
    }

    private class CostTool : AiToolBase<AiMetalInput, CostOutput>
    {
        public int Runs;
        public CostOutput? LastRaw;
        public override string Name => "cost_tool";
        public override string TitleTh => "เครื่องมือทดสอบต้นทุน";
        public override string Description => "d";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView };
        public override string WrapsEndpoint => "X.Y";
        public override TimeSpan CacheTtl => TimeSpan.FromMinutes(1);
        protected override Task<CostOutput> RunAsync(AiMetalInput input, AiToolContext context, CancellationToken ct)
        {
            Runs++;
            return Task.FromResult(LastRaw = new CostOutput { Count = 2, CostThb = 777m, Rows = { new AiMetric { Name = "a", Value = 1 } } });
        }
    }

    private static ClaimsPrincipal Caller(params string[] scopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"), new(ClaimTypes.Name, "boss"), new(AiPrincipalFactory.TokenIdClaim, "5"),
            new(AiPrincipalFactory.TokenKindClaim, AiTokenKinds.OAuth),
        };
        claims.AddRange(scopes.Select(s => new Claim(AiPrincipalFactory.ScopeClaim, s)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "t"));
    }

    private static (AiToolPipeline Pipeline, AiTestEnv Env, CostTool Tool) Create(int limit = 30)
    {
        var env = new AiTestEnv();
        var tool = new CostTool();
        var registry = new AiToolRegistry(new IAiTool[] { tool });
        var cache = new InsightCache(new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions())));
        var pipeline = new AiToolPipeline(registry, env.Users, cache, new AiGlossaryService(env.Context),
            new AiRateLimiter(limit, () => DateTime.UtcNow), env.Context, NullLogger<AiToolPipeline>.Instance, null!);
        return (pipeline, env, tool);
    }

    [Fact]
    public async Task MissingScope_Denied_AndLogged()
    {
        var (p, env, tool) = Create();
        var r = await p.Run(Caller(), "cost_tool", null, default);
        Assert.True(r.IsError);
        Assert.Equal(AiErrorCodes.ScopeDenied, r.ErrorCode);
        Assert.Equal(0, tool.Runs);
        var log = await env.Context.TbtAiAccessLog.SingleAsync();
        Assert.Equal(AiErrorCodes.ScopeDenied, log.ErrorCode);
        Assert.Equal(5, log.TokenId);
    }

    [Fact]
    public async Task MissingPermission_Denied()
    {
        var (p, env, tool) = Create();
        env.Context.TbtUserRole.RemoveRange(env.Context.TbtUserRole);
        env.AddRoleToBoss(9);
        await env.Context.SaveChangesAsync();
        var r = await p.Run(Caller(AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.Equal(AiErrorCodes.PermissionDenied, r.ErrorCode);
        Assert.Equal(0, tool.Runs);
    }

    [Fact]
    public async Task UnknownTool_ShortMessageNoStack()
    {
        var (p, _, _) = Create();
        var r = await p.Run(Caller(AiScopes.ModuleExecutive), "nope", null, default);
        Assert.True(r.IsError);
        Assert.DoesNotContain("   at ", r.Json);
        Assert.Contains("message", r.Json);
    }

    [Fact]
    public async Task SensitiveHidden_WithoutScope_AndCachedObjectUntouched()
    {
        var (p, _, tool) = Create();
        var r = await p.Run(Caller(AiScopes.ModuleExecutive), "cost_tool", "{}", default);
        Assert.False(r.IsError);
        var json = JObject.Parse(r.Json);
        Assert.Equal(JTokenType.Null, json["data"]!["costThb"]!.Type);
        Assert.Equal(2, (int)json["data"]!["count"]!);
        Assert.NotEmpty(json["notes"]!);
        Assert.Equal(777m, tool.LastRaw!.CostThb);

        var r2 = await p.Run(Caller(AiScopes.ModuleExecutive), "cost_tool", "{}", default);
        Assert.Equal(1, tool.Runs);
        Assert.Equal(777m, tool.LastRaw!.CostThb);
        Assert.False(r2.IsError);
    }

    [Fact]
    public async Task SensitiveVisible_WithScopeAndPermission()
    {
        var (p, env, _) = Create();
        env.AddRoleToBoss(9);
        await env.Context.SaveChangesAsync();
        env.GrantData(1, AiScopes.DataCost);
        var r = await p.Run(Caller(AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.Equal(777m, (decimal)JObject.Parse(r.Json)["data"]!["costThb"]!);
    }

    [Fact]
    public async Task ScopeWithoutPermission_StillHidden()
    {
        var (p, _, _) = Create();
        var r = await p.Run(Caller(AiScopes.ModuleExecutive, AiScopes.DataCost), "cost_tool", null, default);
        Assert.Equal(JTokenType.Null, JObject.Parse(r.Json)["data"]!["costThb"]!.Type);
    }

    [Fact]
    public async Task RateLimit_PerUser_SharedAcrossTokens()
    {
        var (p, _, _) = Create(limit: 2);
        var caller = Caller(AiScopes.ModuleExecutive);
        Assert.False((await p.Run(caller, "cost_tool", null, default)).IsError);
        Assert.False((await p.Run(caller, "cost_tool", null, default)).IsError);
        var third = await p.Run(caller, "cost_tool", null, default);
        Assert.Equal(AiErrorCodes.RateLimited, third.ErrorCode);

        // token อื่นของ user เดียวกันใช้เพดานร่วม
        var otherToken = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "boss"),
            new Claim(AiPrincipalFactory.TokenIdClaim, "99"), new Claim(AiPrincipalFactory.ScopeClaim, AiScopes.ModuleExecutive),
        }, "t"));
        Assert.Equal(AiErrorCodes.RateLimited, (await p.Run(otherToken, "cost_tool", null, default)).ErrorCode);
    }

    [Fact]
    public void Clamp_RangeAtMostOneYear_RowCapDefault50()
    {
        var input = new RangedInput { From = DateTime.UtcNow.AddYears(-5), To = DateTime.UtcNow };
        AiToolPipeline.Clamp(input);
        Assert.True((input.To!.Value - input.From!.Value).TotalDays <= AiToolPipeline.MaxRangeDays + 0.001);
        Assert.Equal(50, input.Limit);

        var big = new RangedInput { Limit = 100000 };
        AiToolPipeline.Clamp(big);
        Assert.Equal(AiToolPipeline.MaxRowCap, big.Limit);
    }

    [Fact]
    public async Task AccessLog_PagedFilteredWithThaiTitleAndReason()
    {
        var (p, env, _) = Create();
        var caller = Caller(AiScopes.ModuleExecutive);
        for (var i = 0; i < 3; i++) await p.Run(caller, "cost_tool", null, default);
        await p.Run(Caller(), "cost_tool", null, default);

        var registry = new AiToolRegistry(new IAiTool[] { new CostTool() });
        var svc = env.Connections(registry);

        var page = await svc.AccessLog(1, new AccessLogRequest { Skip = 0, Take = 2 });
        Assert.Equal(4, page.Total);
        Assert.Equal(2, page.Data.Count);
        Assert.All(page.Data, d => Assert.Equal("เครื่องมือทดสอบต้นทุน", d.ToolTitleTh));

        var failed = (await svc.AccessLog(1, new AccessLogRequest { Take = 10 })).Data.Single(d => !d.Success);
        Assert.False(string.IsNullOrEmpty(failed.ErrorMessageTh));
        Assert.Matches("[฀-๿]", failed.ErrorMessageTh!);

        var future = await svc.AccessLog(1, new AccessLogRequest { From = DateTimeOffset.UtcNow.AddDays(1) });
        Assert.Equal(0, future.Total);

        var byToken = await svc.AccessLog(1, new AccessLogRequest { TokenIds = new List<long> { 5 } });
        Assert.Equal(0, byToken.Total);
    }
}
