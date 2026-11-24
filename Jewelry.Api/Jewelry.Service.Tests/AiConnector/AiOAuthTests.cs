using jewelry.Model.AiConnector;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Jewelry.Service.Tests.AiConnector;

public class AiOAuthTests
{
    [Fact]
    public void Pkce_ValidVerifier_Passes()
    {
        Assert.True(AiTokenCrypto.VerifyPkceS256(AiTestEnv.Verifier, AiTestEnv.Challenge(AiTestEnv.Verifier)));
    }

    [Theory]
    [InlineData("wrong-verifier-wrong-verifier-wrong-verifier-wrong")]
    [InlineData("short")]
    [InlineData("")]
    public void Pkce_InvalidVerifier_Fails(string verifier)
    {
        Assert.False(AiTokenCrypto.VerifyPkceS256(verifier, AiTestEnv.Challenge(AiTestEnv.Verifier)));
    }

    [Fact]
    public async Task Exchange_WrongVerifier_Rejected()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code, new string('b', 50)), AiTestEnv.Resource));
        Assert.Equal("invalid_grant", ex.Error);
    }

    [Fact]
    public async Task Exchange_Success_ReturnsValidAccessToken()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        Assert.False(string.IsNullOrEmpty(t.RefreshToken));
        var v = await env.Validator.Validate(t.AccessToken);
        Assert.NotNull(v);
        Assert.Equal("boss", v!.Username);
        Assert.Contains(AiScopes.ModuleExecutive, v.Scopes);
    }

    [Fact]
    public async Task Code_CannotBeUsedTwice_AndRevokesIssuedTokens()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var first = await env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource);

        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource));
        Assert.Equal("invalid_grant", ex.Error);
        Assert.Null(await env.Validator.Validate(first.AccessToken));
    }

    [Fact]
    public async Task Code_Expired_Rejected()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var row = await env.Context.TbtAiAuthCode.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        env.Context.TbtAiAuthCode.Update(row);
        await env.Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(env.CodeRequest(code), AiTestEnv.Resource));
        Assert.Equal("invalid_grant", ex.Error);
    }

    [Fact]
    public async Task Code_StoredAsHashOnly()
    {
        var env = new AiTestEnv();
        var code = await env.ApproveAndGetCode();
        var row = await env.Context.TbtAiAuthCode.SingleAsync();
        Assert.NotEqual(code, row.CodeHash);
        Assert.Equal(AiTokenCrypto.Sha256Hex(code), row.CodeHash);
        Assert.True(row.ExpiresAt <= DateTime.UtcNow.AddMinutes(5.1));
    }

    [Fact]
    public async Task Refresh_Rotates_OldAccessTokenStopsWorking()
    {
        var env = new AiTestEnv();
        var t1 = await env.IssueTokens();
        var t2 = await env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t1.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource);

        Assert.NotEqual(t1.RefreshToken, t2.RefreshToken);
        Assert.NotNull(await env.Validator.Validate(t2.AccessToken));
        Assert.Null(await env.Validator.Validate(t1.AccessToken));
    }

    [Fact]
    public async Task Refresh_ReuseOfOldToken_RevokesWholeFamily()
    {
        var env = new AiTestEnv();
        var t1 = await env.IssueTokens();
        var t2 = await env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t1.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource);

        var ex = await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t1.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource));
        Assert.Equal("invalid_grant", ex.Error);

        Assert.Null(await env.Validator.Validate(t2.AccessToken));
        await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t2.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource));
    }

    [Fact]
    public async Task Refresh_Expired_Rejected()
    {
        var env = new AiTestEnv();
        var t1 = await env.IssueTokens();
        var row = await env.Context.TbtAiToken.SingleAsync();
        row.RefreshExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        env.Context.TbtAiToken.Update(row);
        await env.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = t1.RefreshToken, ClientId = AiTestEnv.ClientUrl }, AiTestEnv.Resource));
    }

    [Fact]
    public async Task RevokedToken_NotAccepted()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        var service = env.Connections();
        var id = (await env.Context.TbtAiToken.SingleAsync()).Id;
        await service.Revoke(1, id);
        Assert.Null(await env.Validator.Validate(t.AccessToken));
    }

    [Fact]
    public async Task User_RoleRemoved_TokenStopsWorking()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        Assert.NotNull(await env.Validator.Validate(t.AccessToken));

        env.Context.TbtUserRole.RemoveRange(env.Context.TbtUserRole);
        await env.Context.SaveChangesAsync();
        Assert.Null(await env.Validator.Validate(t.AccessToken));
    }

    [Fact]
    public async Task User_Deactivated_TokenStopsWorking()
    {
        var env = new AiTestEnv();
        var t = await env.IssueTokens();
        var user = await env.Context.TbtUser.SingleAsync();
        user.IsActive = false;
        env.Context.TbtUser.Update(user);
        await env.Context.SaveChangesAsync();
        Assert.Null(await env.Validator.Validate(t.AccessToken));
    }

    [Fact]
    public async Task WebJwt_SameKeyDifferentAudience_Rejected()
    {
        var env = new AiTestEnv();
        await env.IssueTokens();
        var row = await env.Context.TbtAiToken.SingleAsync();
        var web = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "jewelry-api", audience: "jewelry-client",
            claims: new[] { new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, row.Id.ToString()) },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AiTestEnv.Key)), SecurityAlgorithms.HmacSha256)));
        Assert.Null(await env.Validator.Validate(web));
    }

    [Fact]
    public async Task WebJwt_DifferentKey_Rejected()
    {
        var env = new AiTestEnv();
        await env.IssueTokens();
        var row = await env.Context.TbtAiToken.SingleAsync();
        var forged = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: env.Options.EffectiveIssuer, audience: AiConnectorOptions.Audience,
            claims: new[] { new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, row.Id.ToString()) },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("another-web-jwt-key-0123456789-abcdefgh")), SecurityAlgorithms.HmacSha256)));
        Assert.Null(await env.Validator.Validate(forged));
    }

    [Fact]
    public async Task NoSigningKey_ConnectorDisabled()
    {
        var env = new AiTestEnv(o => o.SigningKey = null);
        Assert.Null(await env.Validator.Validate("dkm_anything"));
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Exchange(new TokenRequest { GrantType = "refresh_token", RefreshToken = "x" }, AiTestEnv.Resource));
    }

    [Fact]
    public async Task Approve_RedirectUriNotRegistered_Rejected()
    {
        var env = new AiTestEnv();
        var p = new ApproveRequest { ResponseType = "code", ClientId = AiTestEnv.ClientUrl, RedirectUri = "https://evil.example.com/cb", CodeChallenge = AiTestEnv.Challenge(AiTestEnv.Verifier), CodeChallengeMethod = "S256", ApprovedScopes = { AiScopes.ModuleExecutive } };
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Approve(p, 1, AiTestEnv.Resource));
        Assert.False(ex.Redirectable);
    }

    [Fact]
    public async Task Authorize_WithoutPkce_Rejected()
    {
        var env = new AiTestEnv();
        var p = env.AuthParams();
        p.CodeChallengeMethod = "plain";
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.ValidateAuthorizeRequest(p, AiTestEnv.Resource));
    }

    [Fact]
    public async Task Approve_DataScopeWithoutPermission_Rejected()
    {
        var env = new AiTestEnv();
        var req = new ApproveRequest { ResponseType = "code", ClientId = AiTestEnv.ClientUrl, RedirectUri = AiTestEnv.Redirect, CodeChallenge = AiTestEnv.Challenge(AiTestEnv.Verifier), CodeChallengeMethod = "S256", ApprovedScopes = { AiScopes.ModuleExecutive, AiScopes.DataCost } };
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Approve(req, 1, AiTestEnv.Resource));
        Assert.Equal("invalid_scope", ex.Error);
    }

    [Fact]
    public async Task Approve_DataScopeWithPermission_Granted()
    {
        var env = new AiTestEnv();
        env.AddRoleToBoss(9);
        await env.Context.SaveChangesAsync();
        var t = await env.IssueTokens(AiScopes.ModuleExecutive, AiScopes.DataCost);
        Assert.Contains(AiScopes.DataCost, AiScopes.Split(t.Scope));
    }

    [Fact]
    public async Task Consent_UserWithoutAiConnect_CannotApprove()
    {
        var env = new AiTestEnv();
        env.Context.TbtUser.Add(new Jewelry.Data.Models.Jewelry.TbtUser { Id = 2, Username = "staff", FirstName = "S", LastName = "T", IsActive = true, Password = "p", Salt = "s", CreateBy = "t", CreateDate = DateTime.UtcNow });
        await env.Context.SaveChangesAsync();

        var c = await env.OAuth.GetConsent(env.AuthParams(), 2, AiTestEnv.Resource);
        Assert.False(c.CanApprove);
        Assert.False(string.IsNullOrEmpty(c.DenyReasonTh));
        await Assert.ThrowsAsync<AiOAuthException>(() => env.OAuth.Approve(new ApproveRequest { ResponseType = "code", ClientId = AiTestEnv.ClientUrl, RedirectUri = AiTestEnv.Redirect, CodeChallenge = AiTestEnv.Challenge(AiTestEnv.Verifier), CodeChallengeMethod = "S256", ApprovedScopes = { AiScopes.ModuleExecutive } }, 2, AiTestEnv.Resource));
    }

    [Fact]
    public async Task Consent_ShowsClientNameAndScopesWithDescriptions()
    {
        var env = new AiTestEnv();
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        Assert.True(c.CanApprove);
        Assert.Equal("Test AI", c.ClientName);
        Assert.Equal("client.example.com", c.RedirectHost);
        var module = Assert.Single(c.Scopes, s => s.Kind == "MODULE");
        Assert.True(module.Required);
        Assert.All(c.Scopes.Where(s => s.Kind == "DATA"), s => Assert.False(s.DefaultChecked));
        Assert.False(c.Scopes.Single(s => s.Code == AiScopes.DataCost).Available);
        Assert.True(c.Scopes.Single(s => s.Code == AiScopes.DataReceivables).Available);
    }
}
