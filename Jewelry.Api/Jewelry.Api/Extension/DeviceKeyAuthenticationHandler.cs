using Jewelry.Service.LabelPrint;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

namespace Jewelry.Api.Extension
{
    public static class DeviceKeyDefaults
    {
        public const string AuthenticationScheme = "DeviceKey";
        public const string HeaderName = "X-Device-Key";
        public const string RoleName = "LabelStation";
    }

    public class DeviceKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public DeviceKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(DeviceKeyDefaults.HeaderName, out var header)
                || string.IsNullOrEmpty(header.ToString()))
            {
                return AuthenticateResult.NoResult();
            }

            var service = Context.RequestServices.GetRequiredService<ILabelStationKeyService>();
            var activeKey = await service.GetActiveKeyCached();
            if (string.IsNullOrEmpty(activeKey))
            {
                return AuthenticateResult.Fail("No active device key");
            }

            var provided = Encoding.UTF8.GetBytes(header.ToString());
            var expected = Encoding.UTF8.GetBytes(activeKey);
            if (!CryptographicOperations.FixedTimeEquals(provided, expected))
            {
                return AuthenticateResult.Fail("Invalid device key");
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "label-station"),
                new Claim(ClaimTypes.Role, DeviceKeyDefaults.RoleName),
            }, Scheme.Name);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";
            await Response.WriteAsync(JsonConvert.SerializeObject(new
            {
                status = 401,
                message = "You are not authorized",
            }));
        }
    }
}
