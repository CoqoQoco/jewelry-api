using jewelry.Model.AiConnector;
using jewelry.Model.Security;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Jewelry.Service.Tests.AiConnector;

public class FakeFetcher : IClientMetadataFetcher
{
    public int Calls { get; private set; }
    public string Json { get; set; } = "";
    public Task<string> Fetch(Uri uri, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(Json);
    }
}

public sealed class AiTestEnv
{
    public const string Key = "unit-test-signing-key-0123456789-abcdef";
    public const string ClientUrl = "https://client.example.com/oauth/client.json";
    public const string Redirect = "https://client.example.com/callback";

    public JewelryContext Context { get; }
    public AiConnectorOptions Options { get; }
    public FakeFetcher Fetcher { get; } = new();
    public IMemoryCache Cache { get; } = new MemoryCache(Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions()));

    public AiTestEnv(Action<AiConnectorOptions>? configure = null)
    {
        var db = new DbContextOptionsBuilder<JewelryContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        Context = new JewelryContext(db);
        Options = new AiConnectorOptions { SigningKey = Key };
        configure?.Invoke(Options);
        Fetcher.Json = ClientJson(ClientUrl, Redirect);
        Seed();
    }

    public static string ClientJson(string clientId, string redirect) =>
        $"{{\"client_id\":\"{clientId}\",\"client_name\":\"Test AI\",\"redirect_uris\":[\"{redirect}\"],\"token_endpoint_auth_method\":\"none\"}}";

    private void Seed()
    {
        var now = DateTime.UtcNow;
        Context.TbmPermission.AddRange(
            new TbmPermission { Id = 1, Code = PermissionCodes.AiConnect, Name = "x", IsActive = true, CreateBy = "t", CreateDate = now },
            new TbmPermission { Id = 2, Code = PermissionCodes.ExecutiveView, Name = "x", IsActive = true, CreateBy = "t", CreateDate = now },
            new TbmPermission { Id = 3, Code = PermissionCodes.SaleViewMargin, Name = "x", IsActive = true, CreateBy = "t", CreateDate = now });
        Context.TbmUserRole.AddRange(
            new TbmUserRole { Id = 8, Name = "Executive", IsActive = true, CreateBy = "t", CreateDate = now },
            new TbmUserRole { Id = 9, Name = "Margin", IsActive = true, CreateBy = "t", CreateDate = now });
        Context.TbtRolePermission.AddRange(
            new TbtRolePermission { RoleId = 8, PermissionId = 1, CreateBy = "t", CreateDate = now },
            new TbtRolePermission { RoleId = 8, PermissionId = 2, CreateBy = "t", CreateDate = now },
            new TbtRolePermission { RoleId = 9, PermissionId = 3, CreateBy = "t", CreateDate = now });
        Context.TbtUser.Add(new TbtUser { Id = 1, Username = "boss", FirstName = "Boss", LastName = "One", IsActive = true, IsNew = false, Password = "p", Salt = "s", CreateBy = "t", CreateDate = now });
        Context.TbtUserRole.Add(new TbtUserRole { UserId = 1, Username = "boss", Role = 8, CreateBy = "t", CreateDate = now });

        Context.TbmAiScope.AddRange(
            new TbmAiScope { Code = AiScopes.ModuleExecutive, NameTh = "exec", Kind = "MODULE", AllowPat = true, IsActive = true, Sort = 1 },
            new TbmAiScope { Code = "module:production", NameTh = "prod", Kind = "MODULE", AllowPat = true, IsActive = false, Sort = 2 },
            new TbmAiScope { Code = AiScopes.DataCustomerNames, NameTh = "cust", Kind = "DATA", AllowPat = false, IsActive = true, Sort = 3 },
            new TbmAiScope { Code = AiScopes.DataCost, NameTh = "cost", Kind = "DATA", AllowPat = false, IsActive = true, Sort = 4 },
            new TbmAiScope { Code = AiScopes.DataReceivables, NameTh = "recv", Kind = "DATA", AllowPat = false, IsActive = true, Sort = 5 });
        Context.SaveChanges();
    }

    public void AddRoleToBoss(int roleId) =>
        Context.TbtUserRole.Add(new TbtUserRole { UserId = 1, Username = "boss", Role = roleId, CreateBy = "t", CreateDate = DateTime.UtcNow });

    public void GrantData(int userId, params string[] scopes)
    {
        foreach (var s in scopes)
            Context.TbtAiUserGrant.Add(new TbtAiUserGrant { UserId = userId, Scope = s, GrantedBy = "dev", GrantedAt = DateTime.UtcNow });
        Context.SaveChanges();
    }

    public void Suspend(int userId)
    {
        Context.TbtAiUserSuspension.Add(new TbtAiUserSuspension { UserId = userId, Source = "manual", ReasonCode = "manual", CreatedBy = "dev", CreatedAt = DateTime.UtcNow });
        Context.SaveChanges();
    }

    public IOptions<AiConnectorOptions> Opt => Microsoft.Extensions.Options.Options.Create(Options);
    public IAiUserDirectory Users => new AiUserDirectory(Context);
    public IAiScopeCatalog Scopes => new AiScopeCatalog(Context);
    public IAiClientRegistry Clients => new AiClientRegistry(Context, Fetcher, Cache);
    public IAiTokenStore Store => new AiTokenStore(Context);
    public OAuthService OAuth => new(Context, Clients, Scopes, Users, Opt, Store);
    public IAiTokenValidator Validator => new AiTokenValidator(Context, Users, Opt, Store);
    public AiConnectionService Connections(Jewelry.Service.AiConnector.Registry.AiToolRegistry? registry = null) =>
        new(Context, Users, Scopes, registry ?? new Jewelry.Service.AiConnector.Registry.AiToolRegistry(Array.Empty<Jewelry.Service.AiConnector.Registry.IAiTool>()), Store);

    public static string Challenge(string verifier) =>
        AiTokenCrypto.Base64Url(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));

    public static readonly string Verifier = new string('a', 50);

    public AuthorizeParams AuthParams(string? scope = null) => new()
    {
        ResponseType = "code",
        ClientId = ClientUrl,
        RedirectUri = Redirect,
        CodeChallenge = Challenge(Verifier),
        CodeChallengeMethod = "S256",
        State = "st",
        Scope = scope,
    };

    public const string Resource = "https://api.example.com/mcp";

    public async Task<string> ApproveAndGetCode(params string[] scopes)
    {
        var req = new ApproveRequest { ResponseType = "code", ClientId = ClientUrl, RedirectUri = Redirect, CodeChallenge = Challenge(Verifier), CodeChallengeMethod = "S256", State = "st" };
        req.ApprovedScopes = scopes.Length == 0 ? new List<string> { AiScopes.ModuleExecutive } : scopes.ToList();
        var res = await OAuth.Approve(req, 1, Resource);
        var uri = new Uri(res.RedirectUrl);
        return System.Web.HttpUtility.ParseQueryString(uri.Query)["code"]!;
    }

    public TokenRequest CodeRequest(string code, string? verifier = null) => new()
    {
        GrantType = "authorization_code",
        Code = code,
        ClientId = ClientUrl,
        RedirectUri = Redirect,
        CodeVerifier = verifier ?? Verifier,
    };

    public async Task<TokenResponse> IssueTokens(params string[] scopes) =>
        await OAuth.Exchange(CodeRequest(await ApproveAndGetCode(scopes)), Resource);
}
