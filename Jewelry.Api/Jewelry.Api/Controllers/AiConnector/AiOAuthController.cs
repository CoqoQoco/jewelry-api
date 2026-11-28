using jewelry.Model.AiConnector;
using Jewelry.Api.Extension;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Claims;

namespace Jewelry.Api.Controllers.AiConnector
{
    // OAuth 2.1 authorization server ของ AI connector
    //  - ส่วน protocol (/oauth/authorize|token|register, /.well-known/*) เรียกโดย AI client → CORS policy แยก
    //  - ส่วน UI (/OAuth/Consent, /OAuth/Authorize/Approve|Deny) เรียกจากหน้าเว็บด้วย JWT เว็บ
    [Route("/")]
    [ApiController]
    public class AiOAuthController : ApiControllerBase
    {
        private readonly IAiOAuthService _oauth;
        private readonly IAiClientRegistry _clients;
        private readonly IAiScopeCatalog _scopes;
        private readonly AiConnectorOptions _options;

        public AiOAuthController(IAiOAuthService oauth, IAiClientRegistry clients, IAiScopeCatalog scopes,
            IOptions<AiConnectorOptions> options, IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _oauth = oauth;
            _clients = clients;
            _scopes = scopes;
            _options = options.Value;
        }

        private int CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;
        private string Resource => AiUrls.McpResource(HttpContext, _options);

        private IActionResult Disabled() =>
            StatusCode(503, new { error = "server_error", message = "AI connector ยังไม่ได้เปิดใช้งาน" });

        // ---------- metadata ----------

        [HttpGet(".well-known/oauth-protected-resource")]
        [HttpGet(".well-known/oauth-protected-resource/mcp")]
        [AllowAnonymous]
        [EnableCors(AiConnectorDefaults.CorsPolicy)]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> ProtectedResourceMetadata()
        {
            if (!_options.IsConfigured) return Disabled();
            var scopes = (await _scopes.Active()).Where(s => s.Kind != "DATA").Select(s => s.Code).ToList();
            return new JsonResult(new
            {
                resource = Resource,
                authorization_servers = new[] { AiUrls.Base(HttpContext, _options) },
                bearer_methods_supported = new[] { "header" },
                scopes_supported = scopes,
            });
        }

        [HttpGet(".well-known/oauth-authorization-server")]
        [AllowAnonymous]
        [EnableCors(AiConnectorDefaults.CorsPolicy)]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> AuthorizationServerMetadata()
        {
            if (!_options.IsConfigured) return Disabled();
            var b = AiUrls.Base(HttpContext, _options);
            var scopes = (await _scopes.Active()).Where(s => s.Kind != "DATA").Select(s => s.Code).ToList();
            return new JsonResult(new
            {
                issuer = b,
                authorization_endpoint = b + "/oauth/authorize",
                token_endpoint = b + "/oauth/token",
                registration_endpoint = b + "/oauth/register",
                response_types_supported = new[] { "code" },
                grant_types_supported = new[] { "authorization_code", "refresh_token" },
                code_challenge_methods_supported = new[] { "S256" },
                token_endpoint_auth_methods_supported = new[] { "none" },
                scopes_supported = scopes,
                client_id_metadata_document_supported = true,
            });
        }

        // ---------- protocol ----------

        [HttpGet("oauth/authorize")]
        [AllowAnonymous]
        [EnableRateLimiting("public")]
        public async Task<IActionResult> Authorize(CancellationToken ct)
        {
            if (!_options.IsConfigured) return Disabled();
            var q = Request.Query;
            var p = new AuthorizeParams
            {
                ResponseType = q["response_type"],
                ClientId = q["client_id"],
                RedirectUri = q["redirect_uri"],
                Scope = q["scope"],
                State = q["state"],
                CodeChallenge = q["code_challenge"],
                CodeChallengeMethod = q["code_challenge_method"],
                Resource = q["resource"],
            };

            try
            {
                await _oauth.ValidateAuthorizeRequest(p, Resource, ct);
            }
            catch (AiOAuthException ex)
            {
                if (ex.Redirectable && !string.IsNullOrEmpty(p.RedirectUri))
                    return Redirect(OAuthService.BuildRedirect(p.RedirectUri, ("error", ex.Error), ("error_description", ex.Description), ("state", p.State)));
                return BadRequest(new { error = ex.Error, error_description = ex.Description, message = ex.Description });
            }

            var ui = _options.UiBaseUrl?.TrimEnd('/');
            if (string.IsNullOrEmpty(ui))
            {
                // fallback localhost เฉพาะ dev — production ต้องตั้ง UiBaseUrl (ไม่งั้น IsConfigured=false ตั้งแต่ต้น)
                if (!_options.RequireExplicitUrls && HttpContext.Request.Host.Host is "localhost" or "127.0.0.1") ui = "http://localhost:2002";
                else return StatusCode(503, new { error = "server_error", error_description = "UiBaseUrl ยังไม่ได้ตั้งค่า" });
            }
            return Redirect(ui + "/oauth/authorize" + Request.QueryString.Value);
        }

        [HttpPost("oauth/token")]
        [AllowAnonymous]
        [EnableCors(AiConnectorDefaults.CorsPolicy)]
        [EnableRateLimiting("public")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Token(CancellationToken ct)
        {
            if (!_options.IsConfigured) return Disabled();
            Response.Headers["Cache-Control"] = "no-store";
            var f = Request.Form;
            var req = new TokenRequest
            {
                GrantType = f["grant_type"],
                Code = f["code"],
                RedirectUri = f["redirect_uri"],
                ClientId = f["client_id"],
                CodeVerifier = f["code_verifier"],
                RefreshToken = f["refresh_token"],
                Resource = f["resource"],
            };

            try
            {
                var t = await _oauth.Exchange(req, Resource, ct);
                return new JsonResult(new
                {
                    access_token = t.AccessToken,
                    token_type = t.TokenType,
                    expires_in = t.ExpiresIn,
                    refresh_token = t.RefreshToken,
                    scope = t.Scope,
                });
            }
            catch (AiOAuthException ex)
            {
                return StatusCode(ex.Error == "server_error" ? 503 : ex.Error == "invalid_client" ? 401 : 400,
                    new { error = ex.Error, error_description = ex.Description });
            }
        }

        [HttpPost("oauth/register")]
        [AllowAnonymous]
        [EnableCors(AiConnectorDefaults.CorsPolicy)]
        [EnableRateLimiting("ai-register")]
        public async Task<IActionResult> Register([FromBody] JObject body)
        {
            if (!_options.IsConfigured) return Disabled();
            DcrRequest req;
            try
            {
                req = new DcrRequest
                {
                    ClientName = StringOrNull(body["client_name"]),
                    RedirectUris = StringListOrNull(body["redirect_uris"]),
                    TokenEndpointAuthMethod = StringOrNull(body["token_endpoint_auth_method"]),
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_client_metadata", error_description = ex.Message });
            }
            try
            {
                var r = await _clients.Register(req);
                return StatusCode(201, new
                {
                    client_id = r.ClientId,
                    client_name = r.ClientName,
                    redirect_uris = r.RedirectUris,
                    token_endpoint_auth_method = r.TokenEndpointAuthMethod,
                    grant_types = r.GrantTypes,
                    response_types = r.ResponseTypes,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_client_metadata", error_description = ex.Message });
            }
        }

        private static string? StringOrNull(JToken? t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            if (t.Type != JTokenType.String) throw new ArgumentException("ชนิดข้อมูลของ metadata ไม่ถูกต้อง");
            return (string?)t;
        }

        private static List<string>? StringListOrNull(JToken? t)
        {
            if (t == null || t.Type == JTokenType.Null) return null;
            if (t is not JArray arr || arr.Any(x => x.Type != JTokenType.String)) throw new ArgumentException("redirect_uris ต้องเป็นรายการข้อความ");
            return arr.Select(x => (string)x!).ToList();
        }

        // ---------- เรียกจาก UI (JWT เว็บ) ----------

        [HttpPost("OAuth/Consent")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ConsentResponse))]
        public async Task<IActionResult> Consent([FromBody] AuthorizeParams request, CancellationToken ct)
        {
            if (!_options.IsConfigured) return Disabled();
            try
            {
                return Ok(await _oauth.GetConsent(request, CurrentUserId, Resource, ct));
            }
            catch (AiOAuthException ex)
            {
                return BadRequest(new { error = ex.Error, message = ex.Description });
            }
        }

        [HttpPost("OAuth/Authorize/Approve")]
        [RequirePermission("ai:connect")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ApproveResponse))]
        public async Task<IActionResult> Approve([FromBody] ApproveRequest request, CancellationToken ct)
        {
            if (!_options.IsConfigured) return Disabled();
            try
            {
                return Ok(await _oauth.Approve(request, CurrentUserId, Resource, ct));
            }
            catch (AiOAuthException ex)
            {
                return BadRequest(new { error = ex.Error, message = ex.Description });
            }
        }

        [HttpPost("OAuth/Authorize/Deny")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ApproveResponse))]
        public async Task<IActionResult> Deny([FromBody] AuthorizeParams request, CancellationToken ct)
        {
            if (!_options.IsConfigured) return Disabled();
            try
            {
                return Ok(await _oauth.Deny(request, Resource, ct));
            }
            catch (AiOAuthException ex)
            {
                return BadRequest(new { error = ex.Error, message = ex.Description });
            }
        }
    }
}
