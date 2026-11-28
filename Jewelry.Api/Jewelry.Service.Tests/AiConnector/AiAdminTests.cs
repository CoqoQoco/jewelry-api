using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Glossary;
using Jewelry.Service.AiConnector.Pipeline;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.Common;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace Jewelry.Service.Tests.AiConnector;

public class AiAdminTests
{
    private class DataTool : AiToolBase<AiMetalInput, AiPipelineTests.CostOutput>
    {
        public override string Name => "cost_tool";
        public override string TitleTh => "ทดสอบ";
        public override string Description => "d";
        public override IReadOnlyList<string> RequiredScopes => new[] { AiScopes.ModuleExecutive };
        public override IReadOnlyList<string> PermissionsAny => new[] { PermissionCodes.ExecutiveView };
        public override string WrapsEndpoint => "X.Y";
        protected override Task<AiPipelineTests.CostOutput> RunAsync(AiMetalInput input, AiToolContext context, CancellationToken ct) =>
            Task.FromResult(new AiPipelineTests.CostOutput { Count = 1, CostThb = 777m });
    }

    private static AiToolPipeline Pipeline(AiTestEnv env) =>
        new(new AiToolRegistry(new IAiTool[] { new DataTool() }), env.Users,
            new InsightCache(new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions()))),
            new AiGlossaryService(env.Context), new AiRateLimiter(100, () => DateTime.UtcNow), env.Context, NullLogger<AiToolPipeline>.Instance, null!);

    private static ClaimsPrincipal Caller(string? kind, params string[] scopes)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1"), new(ClaimTypes.Name, "boss"), new(AiPrincipalFactory.TokenIdClaim, "5") };
        if (kind != null) claims.Add(new Claim(AiPrincipalFactory.TokenKindClaim, kind));
        claims.AddRange(scopes.Select(s => new Claim(AiPrincipalFactory.ScopeClaim, s)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "t"));
    }

    private static AiAdminService Admin(AiTestEnv env, string username = "dev")
    {
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, username) }, "t")) },
        };
        return new AiAdminService(env.Context, http, env.Users, env.Connections(), env.Store, new AiToolRegistry(new IAiTool[] { new DataTool() }));
    }

    private static async Task<string?> CostJson(AiToolPipeline p, ClaimsPrincipal caller) =>
        (await p.Run(caller, "cost_tool", null, default)).Json;

    private static async Task GiveCost(AiTestEnv env)
    {
        env.AddRoleToBoss(9);
        await env.Context.SaveChangesAsync();
    }

    // ---------- effective scopes ----------

    [Fact]
    public async Task OAuthToken_GrantOn_SeesData_AndLogStoresEffectiveScopes()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        env.GrantData(1, AiScopes.DataCost);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.Contains("777", r.Json);
        var log = await env.Context.TbtAiAccessLog.SingleAsync();
        Assert.Contains(AiScopes.DataCost, AiScopes.Split(log.Scopes));
    }

    [Fact]
    public async Task OAuthToken_GrantOff_DataMasked()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.DoesNotContain("777", r.Json);
    }

    [Fact]
    public async Task RevokedGrant_DataMasked()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        env.GrantData(1, AiScopes.DataCost);
        var g = await env.Context.TbtAiUserGrant.SingleAsync();
        g.RevokedAt = DateTime.UtcNow;
        await env.Context.SaveChangesAsync();
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.DoesNotContain("777", r.Json);
    }

    [Fact]
    public async Task Pat_NeverGetsData_EvenWithGrant()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        env.GrantData(1, AiScopes.DataCost);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.Pat, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.DoesNotContain("777", r.Json);
        var log = await env.Context.TbtAiAccessLog.SingleAsync();
        Assert.DoesNotContain(AiScopes.DataCost, AiScopes.Split(log.Scopes));
    }

    [Fact]
    public async Task TokenEmbeddedDataScope_Ignored_WithoutGrant()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive, AiScopes.DataCost), "cost_tool", null, default);
        Assert.DoesNotContain("777", r.Json);
    }

    [Fact]
    public async Task Grant_WithoutBackingPermission_StillMasked()
    {
        var env = new AiTestEnv();
        env.GrantData(1, AiScopes.DataCost);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.DoesNotContain("777", r.Json);
    }

    // ---------- suspension ----------

    [Fact]
    public async Task Suspended_PipelineReturnsSuspended_AndLogged()
    {
        var env = new AiTestEnv();
        env.Suspend(1);
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.True(r.IsError);
        Assert.Equal(AiErrorCodes.Suspended, r.ErrorCode);
        Assert.Contains("บัญชีถูกระงับการใช้ AI ติดต่อผู้ดูแลระบบ", r.Json);
        Assert.Equal(AiErrorCodes.Suspended, (await env.Context.TbtAiAccessLog.SingleAsync()).ErrorCode);
    }

    [Fact]
    public async Task Suspended_DoesNotRevokeTokens_AndLiftRestores()
    {
        var env = new AiTestEnv();
        await env.IssueTokens();
        await Admin(env).Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "test" });
        Assert.All(env.Context.TbtAiToken, t => Assert.Null(t.RevokedAt));
        await Admin(env).Lift(new AiAdminLiftRequest { UserId = 1 });
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.NotEqual(AiErrorCodes.Suspended, r.ErrorCode);
    }

    [Fact]
    public async Task Suspended_ConsentCannotApprove_WithReason()
    {
        var env = new AiTestEnv();
        env.Suspend(1);
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        Assert.False(c.CanApprove);
        Assert.Equal(AiErrorMessages.SuspendedTh, c.DenyReasonTh);
    }

    [Fact]
    public async Task Suspended_ApproveRejected()
    {
        var env = new AiTestEnv();
        env.Suspend(1);
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.ApproveAndGetCode());
        Assert.Equal("access_denied", ex.Error);
    }

    [Fact]
    public async Task Suspended_CodeExchangeRejected()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        env.Suspend(1);
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource));
        Assert.Equal("access_denied", ex.Error);
        Assert.Empty(env.Context.TbtAiToken);
    }

    [Fact]
    public async Task Suspended_RefreshSucceeds_ButPipelineStillSuspended()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        env.Suspend(1);
        var req = new TokenRequest { GrantType = "refresh_token", RefreshToken = t.RefreshToken, ClientId = AiTestEnv.ClientUrl };
        var res = await env.OAuth.Exchange(req, AiTestEnv.Resource);
        Assert.False(string.IsNullOrEmpty(res.AccessToken));
        var r = await Pipeline(env).Run(Caller(AiTokenKinds.OAuth, AiScopes.ModuleExecutive), "cost_tool", null, default);
        Assert.Equal(AiErrorCodes.Suspended, r.ErrorCode);
    }

    [Fact]
    public async Task Suspended_CreatePatRejected()
    {
        var env = new AiTestEnv();
        env.Suspend(1);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            env.Connections().CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = 30 }));
    }

    // ---------- strip data on issue / refresh ----------

    [Fact]
    public async Task Refresh_StripsDataScopeFromLegacyRow()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        var row = await env.Context.TbtAiToken.SingleAsync();
        row.Scopes = AiScopes.ModuleExecutive + " " + AiScopes.DataCost;
        await env.Context.SaveChangesAsync();
        env.Context.ChangeTracker.Clear();

        var req = new TokenRequest { GrantType = "refresh_token", RefreshToken = t.RefreshToken, ClientId = AiTestEnv.ClientUrl };
        var next = await env.OAuth.Exchange(req, AiTestEnv.Resource);
        Assert.Equal(AiScopes.ModuleExecutive, next.Scope);
        var newest = await env.Context.TbtAiToken.OrderByDescending(x => x.Id).FirstAsync();
        Assert.Equal(AiScopes.ModuleExecutive, newest.Scopes);
    }

    [Fact]
    public async Task Authorize_RequestedDataScope_SilentlyDropped()
    {
        var env = new AiTestEnv();
        await env.OAuth.ValidateAuthorizeRequest(env.AuthParams("module:executive data:cost data:nonexistent"), AiTestEnv.Resource);
        var c = await env.OAuth.GetConsent(env.AuthParams("module:executive data:cost"), 1, AiTestEnv.Resource);
        Assert.DoesNotContain(c.Scopes, s => s.Kind == "DATA");
    }

    [Fact]
    public async Task Consent_ListsGrantedCategories_OnlyWhereRoleStillHasPermission()
    {
        var env = new AiTestEnv();
        env.GrantData(1, AiScopes.DataCustomerNames, AiScopes.DataCost);
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        var g = Assert.Single(c.GrantedCategories);
        Assert.Equal(AiScopes.DataCustomerNames, g.Code);
        Assert.Equal("ชื่อลูกค้า", g.NameTh);
    }

    // ---------- admin service ----------

    [Fact]
    public async Task SetGrant_RoleLacksPermission_Rejected()
    {
        var env = new AiTestEnv();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Admin(env).SetGrant(new AiAdminSetGrantRequest { UserId = 1, Scope = AiScopes.DataCost, Enabled = true }));
        Assert.Empty(env.Context.TbtAiUserGrant);
    }

    [Fact]
    public async Task SetGrant_UserLacksAiConnect_Rejected()
    {
        var env = new AiTestEnv();
        env.Context.TbtUser.Add(new TbtUser { Id = 2, Username = "m", FirstName = "M", LastName = "M", IsActive = true, IsNew = false, Password = "p", Salt = "s", CreateBy = "t", CreateDate = DateTime.UtcNow });
        env.Context.TbtUserRole.Add(new TbtUserRole { UserId = 2, Username = "m", Role = 9, CreateBy = "t", CreateDate = DateTime.UtcNow });
        await env.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Admin(env).SetGrant(new AiAdminSetGrantRequest { UserId = 2, Scope = AiScopes.DataCost, Enabled = true }));
    }

    [Fact]
    public async Task SetGrant_UnknownScope_Rejected()
    {
        var env = new AiTestEnv();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Admin(env).SetGrant(new AiAdminSetGrantRequest { UserId = 1, Scope = "module:executive", Enabled = true }));
    }

    [Fact]
    public async Task SetGrant_OnThenOff_AuditsCurrentUser_AndIsIdempotent()
    {
        var env = new AiTestEnv();
        var admin = Admin(env, "dev1");
        var on = new AiAdminSetGrantRequest { UserId = 1, Scope = AiScopes.DataCustomerNames, Enabled = true };
        await admin.SetGrant(on);
        await admin.SetGrant(on);
        var g = await env.Context.TbtAiUserGrant.SingleAsync();
        Assert.Equal("dev1", g.GrantedBy);
        Assert.Null(g.RevokedAt);

        await Admin(env, "dev2").SetGrant(new AiAdminSetGrantRequest { UserId = 1, Scope = AiScopes.DataCustomerNames, Enabled = false });
        g = await env.Context.TbtAiUserGrant.SingleAsync();
        Assert.Equal("dev2", g.RevokedBy);
        Assert.NotNull(g.RevokedAt);

        await admin.SetGrant(on);
        Assert.Equal(2, await env.Context.TbtAiUserGrant.CountAsync());
    }

    [Fact]
    public async Task Suspend_RequiresReason_AndRejectsDuplicate()
    {
        var env = new AiTestEnv();
        var admin = Admin(env, "dev1");
        await Assert.ThrowsAsync<ArgumentException>(() => admin.Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "  " }));
        await admin.Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "ใช้ผิดปกติ" });
        await Assert.ThrowsAsync<AiAdminConflictException>(() => admin.Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "again" }));
        var s = await env.Context.TbtAiUserSuspension.SingleAsync();
        Assert.Equal("manual", s.Source);
        Assert.Equal("dev1", s.CreatedBy);
        Assert.Contains("ใช้ผิดปกติ", s.ReasonDetail);
    }

    [Fact]
    public async Task Suspend_Self_Rejected()
    {
        var env = new AiTestEnv();
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "dev"), new Claim(ClaimTypes.NameIdentifier, "1") }, "t")) },
        };
        var admin = new AiAdminService(env.Context, http, env.Users, env.Connections(), env.Store, new AiToolRegistry(new IAiTool[] { new DataTool() }));
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => admin.Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "x" }));
        Assert.Equal("ระงับบัญชีตัวเองไม่ได้", ex.Message);
        Assert.Empty(env.Context.TbtAiUserSuspension);
    }

    [Fact]
    public async Task Lift_NotSuspended_NotFound_AndAuditsLifter()
    {
        var env = new AiTestEnv();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Admin(env).Lift(new AiAdminLiftRequest { UserId = 1 }));
        await Admin(env, "a").Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "x" });
        await Admin(env, "b").Lift(new AiAdminLiftRequest { UserId = 1 });
        var s = await env.Context.TbtAiUserSuspension.SingleAsync();
        Assert.Equal("b", s.LiftedBy);
        Assert.NotNull(s.LiftedAt);
        await Admin(env, "a").Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "again" });
        Assert.Equal(2, await env.Context.TbtAiUserSuspension.CountAsync());
    }

    [Fact]
    public async Task Users_ListsGrantStateAndSuspension()
    {
        var env = new AiTestEnv();
        await GiveCost(env);
        env.GrantData(1, AiScopes.DataCost);
        await Admin(env).Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "r" });
        var list = await Admin(env).Users();
        var u = Assert.Single(list.Items);
        Assert.True(u.Suspended);
        Assert.Equal("r", u.SuspendedReason);
        Assert.True(u.CanConnect);
        Assert.True(u.Grants.Single(g => g.Scope == AiScopes.DataCost).Enabled);
        Assert.True(u.Grants.Single(g => g.Scope == AiScopes.DataCost).CanGrant);
        Assert.False(u.Grants.Single(g => g.Scope == AiScopes.DataReceivables).Enabled);
    }

    [Fact]
    public async Task RevokeConnection_AnyUsersToken()
    {
        var env = new AiTestEnv();
        await env.IssueTokens();
        var id = (await env.Context.TbtAiToken.SingleAsync()).Id;
        await Admin(env).RevokeConnection(id);
        env.Context.ChangeTracker.Clear();
        Assert.NotNull((await env.Context.TbtAiToken.SingleAsync()).RevokedAt);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Admin(env).RevokeConnection(9999));
    }

    [Fact]
    public async Task AccessLog_AllUsers_FiltersAndIncludesUsernameAndClient()
    {
        var env = new AiTestEnv();
        var now = DateTime.UtcNow;
        await env.IssueTokens();
        var tokenId = (await env.Context.TbtAiToken.SingleAsync()).Id;
        env.Context.TbtAiClient.Add(new TbtAiClient { ClientId = AiTestEnv.ClientUrl, ClientName = "Test AI", Kind = "CIMD", RedirectUris = "[]", CreateDate = now });
        env.Context.TbtAiAccessLog.AddRange(
            new TbtAiAccessLog { UserId = 1, TokenId = tokenId, Tool = "cost_tool", Scopes = "module:executive", CreateDate = now },
            new TbtAiAccessLog { UserId = 2, Tool = "other", Scopes = "module:executive", ErrorCode = AiErrorCodes.Suspended, CreateDate = now.AddDays(-10) });
        await env.Context.SaveChangesAsync();

        var admin = Admin(env);
        var all = await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10 });
        Assert.Equal(2, all.Total);
        var items = all.Data.Cast<AiAdminAccessLogItem>().ToList();
        Assert.Equal("boss", items[0].Username);
        Assert.Equal("Test AI", items[0].ClientName);

        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, UserId = 2 })).Total);
        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, Tool = "cost_tool" })).Total);
        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, DateFrom = now.AddDays(-1) })).Total);
        Assert.Equal(2, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, UserIds = new List<int> { 1, 2 } })).Total);
        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, UserIds = new List<int> { 1 } })).Total);
        Assert.Equal(2, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, Tools = new List<string> { "cost_tool", "other" } })).Total);
        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, Tools = new List<string> { "other" } })).Total);
        Assert.Equal(2, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, UserId = 1, UserIds = new List<int> { 2 } })).Total);
        Assert.Equal(2, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, Tool = "cost_tool", Tools = new List<string> { "other" } })).Total);
        Assert.Equal(2, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, UserIds = new List<int>(), Tools = new List<string>() })).Total);
        Assert.Equal(1, (await admin.AccessLog(new AiAdminAccessLogRequest { Take = 10, DateTo = now.AddDays(-5) })).Total);
        var paged = await admin.AccessLog(new AiAdminAccessLogRequest { Take = 1, Skip = 1 });
        Assert.Equal(2, paged.Total);
        Assert.Single(paged.Data.Cast<AiAdminAccessLogItem>());
    }

    [Fact]
    public async Task Suspensions_ListsWithFilters()
    {
        var env = new AiTestEnv();
        await Admin(env).Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "r" });
        await Admin(env).Lift(new AiAdminLiftRequest { UserId = 1 });
        await Admin(env).Suspend(new AiAdminSuspendRequest { UserId = 1, Reason = "r2" });
        var admin = Admin(env);
        Assert.Equal(2, (await admin.Suspensions(new AiAdminSuspensionsRequest { Take = 10 })).Total);
        var active = await admin.Suspensions(new AiAdminSuspensionsRequest { Take = 10, Active = true });
        var item = Assert.Single(active.Data.Cast<AiAdminSuspensionItem>());
        Assert.Equal("r2", item.ReasonText);
        Assert.Equal("boss", item.Username);
    }
}
