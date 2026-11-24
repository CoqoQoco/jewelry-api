using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Jewelry.Api.Extension
{
    public static class AiConnectorDefaults
    {
        public const string AuthenticationScheme = "AiConnector";
        public const string CorsPolicy = "AiConnector";
        public const string McpPath = "/mcp";
    }

    public static class AiUrls
    {
        public static string Base(HttpContext http, AiConnectorOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl)) return options.BaseUrl!.TrimEnd('/');
            var host = http.Request.Host;
            var scheme = host.Host is "localhost" or "127.0.0.1" ? http.Request.Scheme : "https";
            return $"{scheme}://{host}";
        }

        public static string McpResource(HttpContext http, AiConnectorOptions options) => Base(http, options) + AiConnectorDefaults.McpPath;
    }

    // scheme แยกของ /mcp — ไม่ใช่ JwtBearer ของเว็บ: key / audience / issuer / ตาราง token แยกกันหมด
    public class AiConnectorAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AiConnectorOptions _aiOptions;

        public AiConnectorAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<AiConnectorOptions> aiOptions)
            : base(options, logger, encoder)
        {
            _aiOptions = aiOptions.Value;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!_aiOptions.IsConfigured) return AuthenticateResult.NoResult();

            var header = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();

            var validator = Context.RequestServices.GetRequiredService<IAiTokenValidator>();
            var validated = await validator.Validate(header.Substring(7).Trim());
            if (validated == null) return AuthenticateResult.Fail("Invalid token");

            var principal = AiPrincipalFactory.Create(validated);
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            var metadata = AiUrls.Base(Context, _aiOptions) + "/.well-known/oauth-protected-resource";
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers["WWW-Authenticate"] = $"Bearer resource_metadata=\"{metadata}\"";
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
