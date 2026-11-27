using jewelry.Model.AiConnector;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Tests.AiConnector;

public class AiOptionalModuleConsentTests
{
    private static AiTestEnv EnvWithProductionActive()
    {
        var env = new AiTestEnv();
        var row = env.Context.TbmAiScope.Single(s => s.Code == AiScopes.ModuleProduction);
        row.IsActive = true;
        env.Context.SaveChanges();
        return env;
    }

    private static ApproveRequest Approve(AiTestEnv env, params string[] scopes) => new()
    {
        ResponseType = "code",
        ClientId = AiTestEnv.ClientUrl,
        RedirectUri = AiTestEnv.Redirect,
        CodeChallenge = AiTestEnv.Challenge(AiTestEnv.Verifier),
        CodeChallengeMethod = "S256",
        ApprovedScopes = scopes.ToList(),
    };

    [Fact]
    public async Task Consent_ShowsUnrequestedActiveModuleAsOptionalUnchecked()
    {
        var env = EnvWithProductionActive();
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        var exec = c.Scopes.Single(s => s.Code == AiScopes.ModuleExecutive);
        Assert.True(exec.Required);
        var prod = c.Scopes.Single(s => s.Code == AiScopes.ModuleProduction);
        Assert.Equal("MODULE", prod.Kind);
        Assert.False(prod.Required);
        Assert.False(prod.DefaultChecked);
        Assert.True(prod.Available);
    }

    [Fact]
    public async Task Consent_InactiveModuleNotShown()
    {
        var env = new AiTestEnv();
        var c = await env.OAuth.GetConsent(env.AuthParams(), 1, AiTestEnv.Resource);
        Assert.DoesNotContain(c.Scopes, s => s.Code == AiScopes.ModuleProduction);
    }

    [Fact]
    public async Task Approve_TickedOptionalModule_IsGrantedInToken()
    {
        var env = EnvWithProductionActive();
        var t = await env.IssueTokens(AiScopes.ModuleExecutive, AiScopes.ModuleProduction);
        var scopes = AiScopes.Split(t.Scope);
        Assert.Contains(AiScopes.ModuleProduction, scopes);
        Assert.Contains(AiScopes.ModuleExecutive, scopes);
    }

    [Fact]
    public async Task Approve_UnticketedOptionalModule_NotGranted()
    {
        var env = EnvWithProductionActive();
        var t = await env.IssueTokens(AiScopes.ModuleExecutive);
        Assert.DoesNotContain(AiScopes.ModuleProduction, AiScopes.Split(t.Scope));
    }

    [Fact]
    public async Task Approve_InactiveModule_Rejected()
    {
        var env = new AiTestEnv();
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Approve(Approve(env, AiScopes.ModuleExecutive, AiScopes.ModuleProduction), 1, AiTestEnv.Resource));
        Assert.Equal("invalid_scope", ex.Error);
    }

    [Fact]
    public async Task Approve_UnknownModule_Rejected()
    {
        var env = EnvWithProductionActive();
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Approve(Approve(env, AiScopes.ModuleExecutive, "module:nope"), 1, AiTestEnv.Resource));
        Assert.Equal("invalid_scope", ex.Error);
    }

    [Fact]
    public async Task Approve_RequiredModuleStillMandatory()
    {
        var env = EnvWithProductionActive();
        var ex = await Assert.ThrowsAsync<AiOAuthException>(() =>
            env.OAuth.Approve(Approve(env, AiScopes.ModuleProduction), 1, AiTestEnv.Resource));
        Assert.Equal("invalid_scope", ex.Error);
    }
}
