using Jewelry.Service.AiConnector.Auth;

namespace Jewelry.Service.Tests.AiConnector;

public class AiChatGptCimdTests
{
    private const string ClientId = "https://chatgpt.com/oauth/imBMyqRkYGBb/client.json";
    private const string Redirect = "https://chatgpt.com/connector/oauth/imBMyqRkYGBb";

    private static string Doc(string authFields) =>
        "{\"client_id\":\"" + ClientId + "\",\"client_uri\":\"https://chatgpt.com/\",\"redirect_uris\":[\"" + Redirect + "\"]," + authFields +
        "\"grant_types\":[\"authorization_code\",\"refresh_token\"],\"response_types\":[\"code\"],\"client_name\":\"ChatGPT\"," +
        "\"logo_uri\":\"https://persistent.oaistatic.com/sonic/misc/openai-logo.png\",\"token_endpoint_auth_signing_alg\":\"RS256\",\"jwks_uri\":\"https://chatgpt.com/oauth/jwks.json\"}";

    [Fact]
    public void RealChatGptDocument_IsAccepted()
    {
        var json = Doc("\"token_endpoint_auth_method\":\"private_key_jwt\",\"token_endpoint_auth_methods_supported\":[\"none\",\"private_key_jwt\"],");
        var client = AiClientRegistry.ParseMetadata(ClientId, json);
        Assert.NotNull(client);
        Assert.Equal("ChatGPT", client!.ClientName);
        Assert.True(client.Verified);
    }

    [Fact]
    public void PrivateKeyJwtOnly_IsRejected()
    {
        var json = Doc("\"token_endpoint_auth_method\":\"private_key_jwt\",\"token_endpoint_auth_methods_supported\":[\"private_key_jwt\"],");
        Assert.Null(AiClientRegistry.ParseMetadata(ClientId, json, out var reason));
        Assert.Equal("token_endpoint_auth_method_not_none", reason);
    }

    [Fact]
    public void NoMethodFields_IsAccepted()
    {
        Assert.NotNull(AiClientRegistry.ParseMetadata(ClientId, Doc("")));
    }

    [Theory]
    [InlineData("https://chatgpt.com/connector/oauth/imBMyqRkYGBb", true)]
    [InlineData("https://chatgpt.com/connector/oauth/a/b", false)]
    [InlineData("https://chatgpt.com/connector/oauth/../x", false)]
    [InlineData("https://chatgpt.com/connector/oauth/abc?x=1", false)]
    [InlineData("http://chatgpt.com/connector/oauth/abc", false)]
    [InlineData("https://chatgpt.com.evil.com/connector/oauth/abc", false)]
    [InlineData("https://chatgpt.com/connector/oauth/", false)]
    [InlineData("https://chatgpt.com:8443/connector/oauth/abc", false)]
    public void IsKnownRedirect_ChatGptConnector(string uri, bool expected)
    {
        Assert.Equal(expected, SsrfGuard.IsKnownRedirect(uri));
    }
}
