using jewelry.Model.AiConnector;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Registry;
using Jewelry.Service.AiConnector.Tools.Executive;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace Jewelry.Service.Tests.AiConnector;

public class AiHardeningTests
{
    // ---- 1: digest money keys ----

    private static readonly string[] AlwaysMoney =
    {
        "excessMoney", "avgMonthlyExcessMoney", "amountThb", "outstandingThb", "overdueThb", "avgWages", "wagePerJob",
        "projectedWages", "medianPerJob", "netPerMonth", "net", "fromValue", "toValue", "costPerGram", "someFutureKey",
    };

    [Fact]
    public void DigestKeys_Unknown_And_MoneyKeys_AreNotInPlainMetrics()
    {
        foreach (var k in AlwaysMoney) Assert.True(ExecutiveDigestTool.IsMoneyKey(k), k);
    }

    [Theory]
    [InlineData("count")] [InlineData("percent")] [InlineData("days")] [InlineData("excessGram")] [InlineData("backlogMonths")]
    [InlineData("coveragePercent")] [InlineData("onTimePercent")] [InlineData("workerCount")] [InlineData("queueDays")]
    public void DigestKeys_KnownSafe_StayPlain(string key) => Assert.False(ExecutiveDigestTool.IsMoneyKey(key));

    private static string? FindFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var p = Path.Combine(dir.FullName, relative);
            if (File.Exists(p)) return p;
            dir = dir.Parent;
        }
        return null;
    }

    [Fact]
    public void DigestKeys_EveryKeyEmittedByRuleEngines_AllMoneyLikeKeysAreMasked()
    {
        var files = new[]
        {
            "Jewelry.Service/Production/Insight/ProductionInsightRuleEngine.cs", "Jewelry.Service/Sale/Insight/SalesInsightRuleEngine.cs",
            "Jewelry.Service/Stock/Insight/StockInsightRuleEngine.cs", "Jewelry.Service/Material/Insight/MaterialInsightRuleEngine.cs",
            "Jewelry.Service/Report/Executive/ExecutiveDataQualityRuleEngine.cs",
        };
        var keys = new HashSet<string>();
        foreach (var f in files)
        {
            var path = FindFile(f);
            Assert.True(path != null, "ไม่พบ " + f);
            foreach (Match m in Regex.Matches(File.ReadAllText(path!), "\\[\"([A-Za-z0-9_]+)\"\\]\\s*=")) keys.Add(m.Groups[1].Value);
        }
        Assert.True(keys.Count > 50);
        Assert.Contains("excessMoney", keys);
        Assert.Contains("avgMonthlyExcessMoney", keys);

        var moneyLike = new[] { "money", "thb", "amount", "wage", "cost", "price", "outstanding" };
        foreach (var k in keys.Where(k => moneyLike.Any(w => k.ToLowerInvariant().Contains(w))))
        {
            Assert.True(ExecutiveDigestTool.IsMoneyKey(k), k);
            var item = ExecutiveDigestTool.MapItem(new jewelry.Model.Report.Executive.Digest.TopItem
            {
                Domain = "stock", Code = "X", Params = new Dictionary<string, object> { [k] = 5m },
            });
            Assert.Empty(item.Metrics);
            Assert.Single(item.CostMetrics);
        }
    }

    // ---- 13: read-only tools ----

    [Fact]
    public void Tools_SourceNeverCallsWriteMethods()
    {
        var dir = FindFile("Jewelry.Service/AiConnector/Tools/Executive/ExecutiveSummaryTool.cs");
        Assert.NotNull(dir);
        var files = Directory.GetFiles(Path.GetDirectoryName(dir!)!, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(files);
        foreach (var f in files)
        {
            var bad = Regex.Matches(File.ReadAllText(f), @"\.(Save|Update|Delete|Create|Insert|Remove)\w*\s*\(");
            Assert.True(bad.Count == 0, f + ": " + string.Join(",", bad.Select(b => b.Value)));
        }
    }

    // ---- 12 ----

    private class NoSetter
    {
        [AiSensitive(SensitiveCategory.Cost)] public decimal? X { get; }
    }

    [Fact]
    public void AiDtoValidator_SensitiveNeedsPublicSetter() =>
        Assert.Contains(AiDtoValidator.Validate(typeof(NoSetter)), e => e.Contains("public setter"));

    // ---- 3/4/5 redirect + verification ----

    [Theory]
    [InlineData("https://claude.ai/api/mcp/auth_callback", true)]
    [InlineData("https://chatgpt.com/connector_platform_oauth_redirect", true)]
    [InlineData("http://localhost:6274/oauth/callback", true)]
    [InlineData("cursor://anysphere.cursor-mcp/oauth/callback", true)]
    [InlineData("vscode://x/y", true)]
    [InlineData("vscode-insiders://x/y", true)]
    [InlineData("claude://x", true)]
    [InlineData("https://claude.ai/other", false)]
    [InlineData("https://evil.example.com/cb", false)]
    [InlineData("com.example.app://cb", false)]
    public void KnownRedirects(string uri, bool known) => Assert.Equal(known, SsrfGuard.IsKnownRedirect(uri));

    [Theory]
    [InlineData("com.example.app://cb", true)]
    [InlineData("myapp://cb", false)]
    [InlineData("ms-office://cb", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,x", false)]
    [InlineData("claude://cb", true)]
    [InlineData("http://example.com/cb", false)]
    public void RedirectScheme_IsAllowlist(string uri, bool ok) => Assert.Equal(ok, SsrfGuard.IsValidRedirectUri(uri));

    [Fact]
    public async Task Consent_UnknownClient_NotVerified_WithFullRedirects()
    {
        var env = new AiTestEnv();
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        Assert.False(c.ClientVerified);
        Assert.Equal(AiTestEnv.ClientUrl, c.ClientId);
        Assert.Equal(AiTestEnv.Redirect, c.RedirectUri);
        Assert.Equal(new[] { AiTestEnv.Redirect }, c.RedirectUris);
    }

    [Fact]
    public async Task Consent_KnownClient_Verified()
    {
        var env = new AiTestEnv();
        const string id = "https://claude.ai/oauth/client.json";
        env.Fetcher.Json = AiTestEnv.ClientJson(id, "https://claude.ai/api/mcp/auth_callback");
        var p = env.AuthParams();
        p.ClientId = id;
        p.RedirectUri = "https://claude.ai/api/mcp/auth_callback";
        Assert.True((await env.OAuth.GetConsent(p, 1, AiTestEnv.Resource)).ClientVerified);
    }

    [Fact]
    public async Task AuthorizeError_UnverifiedClient_NotRedirectable_VerifiedIs()
    {
        var env = new AiTestEnv();
        var p = env.AuthParams();
        p.CodeChallengeMethod = "plain";
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.ValidateAuthorizeRequest(p, AiTestEnv.Resource));
        Assert.False(ex.Redirectable);

        const string id = "https://claude.ai/oauth/client.json";
        env.Fetcher.Json = AiTestEnv.ClientJson(id, "https://claude.ai/api/mcp/auth_callback");
        p.ClientId = id;
        p.RedirectUri = "https://claude.ai/api/mcp/auth_callback";
        var ex2 = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.ValidateAuthorizeRequest(p, AiTestEnv.Resource));
        Assert.True(ex2.Redirectable);
    }

    // ---- 3/6/7 CIMD ----

    [Fact]
    public void Cimd_RedirectMustBeSameOriginAsClientId()
    {
        Assert.Null(AiClientRegistry.ParseMetadata(AiTestEnv.ClientUrl, AiTestEnv.ClientJson(AiTestEnv.ClientUrl, "https://other.example.com/cb")));
        Assert.NotNull(AiClientRegistry.ParseMetadata(AiTestEnv.ClientUrl, AiTestEnv.ClientJson(AiTestEnv.ClientUrl, "http://127.0.0.1:8080/cb")));
        Assert.NotNull(AiClientRegistry.ParseMetadata(AiTestEnv.ClientUrl, AiTestEnv.ClientJson(AiTestEnv.ClientUrl, "cursor://a/b")));
    }

    [Theory]
    [InlineData("{\"client_id\":5,\"client_name\":\"x\",\"redirect_uris\":[\"https://client.example.com/cb\"]}")]
    [InlineData("{\"client_id\":\"https://client.example.com/oauth/client.json\",\"client_name\":{\"a\":1},\"redirect_uris\":[\"https://client.example.com/cb\"]}")]
    [InlineData("{\"client_id\":\"https://client.example.com/oauth/client.json\",\"client_name\":\"x\",\"redirect_uris\":\"https://client.example.com/cb\"}")]
    [InlineData("{\"client_id\":\"https://client.example.com/oauth/client.json\",\"client_name\":\"x\",\"redirect_uris\":[1,2]}")]
    [InlineData("{\"client_id\":\"https://client.example.com/oauth/client.json\",\"client_name\":\"x\",\"redirect_uris\":[\"https://client.example.com/cb\"],\"token_endpoint_auth_method\":5}")]
    [InlineData("not json")]
    public void Cimd_WrongTypes_ReturnNullNotThrow(string json) =>
        Assert.Null(AiClientRegistry.ParseMetadata(AiTestEnv.ClientUrl, json));

    [Fact]
    public async Task Cimd_NotPersistedAtResolve_OnlyAfterApprove()
    {
        var env = new AiTestEnv();
        await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        Assert.Empty(env.Context.TbtAiClient);
        await env.ApproveAndGetCode();
        Assert.Single(env.Context.TbtAiClient);
        await env.ApproveAndGetCode();
        Assert.Single(env.Context.TbtAiClient);
    }

    [Fact]
    public async Task Cimd_FetchFailure_NegativeCached()
    {
        var env = new AiTestEnv();
        env.Fetcher.Json = "garbage";
        Assert.Null(await env.Clients.Resolve(AiTestEnv.ClientUrl));
        Assert.Null(await env.Clients.Resolve(AiTestEnv.ClientUrl));
        Assert.Equal(1, env.Fetcher.Calls);
    }

    [Theory]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("64:ff9b::a00:1")]
    [InlineData("64:ff9b::7f00:1")]
    [InlineData("2002:0a00:0001::1")]
    [InlineData("2002:c0a8:0101::1")]
    [InlineData("::10.0.0.1")]
    [InlineData("::127.0.0.1")]
    [InlineData("2001:0:4136:e378:8000:63bf:3fff:fdd2")]
    public void Ipv6EmbeddingInternalIpv4_Rejected(string ip) => Assert.False(SsrfGuard.IsPublicAddress(IPAddress.Parse(ip)));

    [Fact]
    public void Ipv6EmbeddingPublicIpv4_Allowed() => Assert.True(SsrfGuard.IsPublicAddress(IPAddress.Parse("64:ff9b::808:808")));

    // ---- 10 refresh requires client_id ----

    [Fact]
    public async Task Refresh_RequiresMatchingClientId()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        var none = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t.RefreshToken }, AiTestEnv.Resource));
        Assert.Equal("invalid_request", none.Error);
        var wrong = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t.RefreshToken, ClientId = "other" }, AiTestEnv.Resource));
        Assert.Equal("invalid_grant", wrong.Error);
    }

    // ---- 2: family kill ----

    [Fact]
    public async Task KillFamily_BlocksLaterRefreshEvenWhenAllRowsRotated()
    {
        var env = new AiTestEnv();
        var t1 = await env.IssueTokens();
        var t2 = await env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t1.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource);
        var family = (await env.Context.TbtAiToken.FirstAsync()).FamilyId!;
        Assert.False(await env.Store.IsFamilyKilled(family));
        await env.Store.KillFamily(family, DateTime.UtcNow);
        Assert.True(await env.Store.IsFamilyKilled(family));
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t2.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource));
    }

    [Fact]
    public async Task ClaimAuthCode_OnlyOnce()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var hash = AiTokenCrypto.Sha256Hex(code);
        Assert.True(await env.Store.ClaimAuthCode(hash, DateTime.UtcNow));
        Assert.False(await env.Store.ClaimAuthCode(hash, DateTime.UtcNow));
    }

    // ---- 8 PAT limit ----

    [Fact]
    public async Task Pat_MaxFiveActivePerUser()
    {
        var env = new AiTestEnv();
        var svc = env.Connections();
        for (var i = 0; i < 5; i++) await svc.CreatePat(1, new CreatePatRequest { Name = "p" + i, ExpiresInDays = 30 });
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.CreatePat(1, new CreatePatRequest { Name = "p6", ExpiresInDays = 30 }));
        Assert.Matches("[\u0E00-\u0E7F]", ex.Message);
        var first = (await env.Context.TbtAiToken.FirstAsync()).Id;
        await svc.Revoke(1, first);
        await svc.CreatePat(1, new CreatePatRequest { Name = "again", ExpiresInDays = 30 });
    }

    // ---- 9 production urls ----

    [Fact]
    public void Production_RequiresBaseAndUiUrl()
    {
        var o = new AiConnectorOptions { SigningKey = AiTestEnv.Key, RequireExplicitUrls = true };
        Assert.False(o.IsConfigured);
        o.BaseUrl = "https://api.example.com";
        Assert.False(o.IsConfigured);
        o.UiBaseUrl = "https://app.example.com";
        Assert.True(o.IsConfigured);
        Assert.True(new AiConnectorOptions { SigningKey = AiTestEnv.Key }.IsConfigured);
    }

    [Fact]
    public async Task UsedCode_WithWrongVerifier_DoesNotKillFamily()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var t = await env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource);
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code, new string('z', 50)), AiTestEnv.Resource));
        Assert.NotNull(await env.Validator.Validate(t.AccessToken));
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource));
        Assert.Null(await env.Validator.Validate(t.AccessToken));
    }

    [Fact]
    public async Task Deny_UnverifiedClient_NoRedirectUrl_VerifiedClient_Redirects()
    {
        var env = new AiTestEnv();
        Assert.Null((await env.OAuth.Deny(env.AuthParams(), AiTestEnv.Resource)).RedirectUrl);

        const string id = "https://claude.ai/oauth/client.json";
        env.Fetcher.Json = AiTestEnv.ClientJson(id, "https://claude.ai/api/mcp/auth_callback");
        var p = env.AuthParams();
        p.ClientId = id;
        p.RedirectUri = "https://claude.ai/api/mcp/auth_callback";
        var r = await env.OAuth.Deny(p, AiTestEnv.Resource);
        Assert.Contains("error=access_denied", r.RedirectUrl);
    }
}
