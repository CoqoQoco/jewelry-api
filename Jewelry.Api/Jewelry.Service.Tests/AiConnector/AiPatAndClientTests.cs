using jewelry.Model.AiConnector;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Jewelry.Service.AiConnector.Registry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.AiConnector;

public class AiPatAndClientTests
{
    private static AiConnectionService Service(AiTestEnv env) =>
        env.Connections();

    [Theory]
    [InlineData(AiScopes.DataCost)]
    [InlineData(AiScopes.DataCustomerNames)]
    [InlineData("data:anything")]
    public async Task Pat_CannotRequestDataScopes(string scope)
    {
        var env = new AiTestEnv();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service(env).CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = 30, Scopes = new List<string> { AiScopes.ModuleExecutive, scope } }));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(365)]
    public async Task Pat_ExpiryMustBe30_60_90(int days)
    {
        var env = new AiTestEnv();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(env).CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = days }));
    }

    [Fact]
    public async Task Pat_StoredAsHash_ValidatesAndShowsOnce()
    {
        var env = new AiTestEnv();
        var r = await Service(env).CreatePat(1, new CreatePatRequest { Name = "cursor", ExpiresInDays = 90 });
        Assert.StartsWith("dkm_", r.Token);
        var row = await env.Context.TbtAiToken.SingleAsync();
        Assert.Equal(AiTokenCrypto.Sha256Hex(r.Token), row.TokenHash);
        Assert.DoesNotContain(r.Token, row.Name + row.Scopes);

        var v = await env.Validator.Validate(r.Token);
        Assert.NotNull(v);
        Assert.Equal(new[] { AiScopes.ModuleExecutive }, v!.Scopes);

        var list = await Service(env).List(1);
        var item = Assert.Single(list.Items);
        Assert.NotNull(item.LastUsedAt);
        Assert.True(item.ExpiresAt > DateTime.UtcNow.AddDays(89));
    }

    [Fact]
    public async Task Pat_NeverUsed_LastUsedIsNull()
    {
        var env = new AiTestEnv();
        await Service(env).CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = 30 });
        var item = Assert.Single((await Service(env).List(1)).Items);
        Assert.Null(item.LastUsedAt);
    }

    [Fact]
    public async Task Pat_Expired_Rejected()
    {
        var env = new AiTestEnv();
        var r = await Service(env).CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = 30 });
        var row = await env.Context.TbtAiToken.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        env.Context.TbtAiToken.Update(row);
        await env.Context.SaveChangesAsync();
        Assert.Null(await env.Validator.Validate(r.Token));
    }

    [Fact]
    public async Task Revoke_OtherUsersConnection_NotFound()
    {
        var env = new AiTestEnv();
        await Service(env).CreatePat(1, new CreatePatRequest { Name = "x", ExpiresInDays = 30 });
        var id = (await env.Context.TbtAiToken.SingleAsync()).Id;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => Service(env).Revoke(99, id));
    }

    [Theory]
    [InlineData("http://client.example.com/a.json")]
    [InlineData("https://localhost/a.json")]
    [InlineData("https://127.0.0.1/a.json")]
    [InlineData("https://10.0.0.5/a.json")]
    [InlineData("https://[::1]/a.json")]
    [InlineData("https://intranet/a.json")]
    [InlineData("https://printer.local/a.json")]
    [InlineData("https://user:pw@client.example.com/a.json")]
    [InlineData("https://client.example.com:8443/a.json")]
    [InlineData("https://client.example.com/")]
    public void Cimd_InternalOrUnsafeUrls_Rejected(string url)
    {
        Assert.False(SsrfGuard.IsAllowedMetadataUrl(url, out _));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    [InlineData("::ffff:10.0.0.1")]
    public void InternalAddresses_NotPublic(string ip)
    {
        Assert.False(SsrfGuard.IsPublicAddress(System.Net.IPAddress.Parse(ip)));
    }

    [Fact]
    public void PublicAddress_Allowed()
    {
        Assert.True(SsrfGuard.IsPublicAddress(System.Net.IPAddress.Parse("8.8.8.8")));
    }

    [Fact]
    public async Task Cimd_InternalUrl_NeverFetched()
    {
        var env = new AiTestEnv();
        Assert.Null(await env.Clients.Resolve("https://localhost/client.json"));
        Assert.Null(await env.Clients.Resolve("https://192.168.1.10/client.json"));
        Assert.Equal(0, env.Fetcher.Calls);
    }

    [Fact]
    public async Task Cimd_ClientIdMismatch_Rejected()
    {
        var env = new AiTestEnv();
        env.Fetcher.Json = AiTestEnv.ClientJson("https://other.example.com/x.json", AiTestEnv.Redirect);
        Assert.Null(await env.Clients.Resolve(AiTestEnv.ClientUrl));
    }

    [Fact]
    public async Task Cimd_Valid_ResolvesAndIsCached()
    {
        var env = new AiTestEnv();
        var c = await env.Clients.Resolve(AiTestEnv.ClientUrl);
        Assert.Equal("Test AI", c!.ClientName);
        await env.Clients.Resolve(AiTestEnv.ClientUrl);
        Assert.Equal(1, env.Fetcher.Calls);
    }

    [Fact]
    public async Task Dcr_RegistersPublicClient_RejectsBadRedirect()
    {
        var env = new AiTestEnv();
        var ok = await env.Clients.Register(new DcrRequest { ClientName = "Cursor", RedirectUris = new List<string> { "cursor://anysphere.cursor-mcp/oauth/callback" } });
        Assert.StartsWith("dkc_", ok.ClientId);
        Assert.NotNull(await env.Clients.Resolve(ok.ClientId));

        await Assert.ThrowsAsync<ArgumentException>(() => env.Clients.Register(new DcrRequest { ClientName = "x", RedirectUris = new List<string> { "javascript:alert(1)" } }));
        await Assert.ThrowsAsync<ArgumentException>(() => env.Clients.Register(new DcrRequest { ClientName = "x", RedirectUris = new List<string> { "http://evil.example.com/cb" } }));
    }

    [Fact]
    public void RedirectMatching_ExactOnly_LoopbackPortMayVary()
    {
        var reg = new[] { "https://a.example.com/cb", "http://127.0.0.1:3000/cb" };
        Assert.True(SsrfGuard.RedirectMatches("https://a.example.com/cb", reg));
        Assert.False(SsrfGuard.RedirectMatches("https://a.example.com/cb/", reg));
        Assert.False(SsrfGuard.RedirectMatches("https://a.example.com/cb?x=1", reg));
        Assert.True(SsrfGuard.RedirectMatches("http://127.0.0.1:54321/cb", reg));
        Assert.False(SsrfGuard.RedirectMatches("http://127.0.0.1:54321/other", reg));
    }
}
