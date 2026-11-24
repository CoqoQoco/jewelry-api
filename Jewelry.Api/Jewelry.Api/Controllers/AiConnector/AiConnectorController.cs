using jewelry.Model.AiConnector;
using Jewelry.Api.Extension;
using Jewelry.Service.AiConnector;
using Jewelry.Service.AiConnector.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace Jewelry.Api.Controllers.AiConnector
{
    // หน้า /executive/ai-connect — จัดการการเชื่อมต่อ (เห็น/จัดการได้เฉพาะของตัวเอง)
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    [RequirePermission("ai:connect")]
    public class AiConnectorController : ApiControllerBase
    {
        private readonly IAiConnectionService _service;
        private readonly AiConnectorOptions _options;

        public AiConnectorController(IAiConnectionService service, IOptions<AiConnectorOptions> options, IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
            _options = options.Value;
        }

        private int CurrentUserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        [Route("Info")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(McpInfoResponse))]
        public async Task<IActionResult> Info()
        {
            return Ok(new McpInfoResponse
            {
                McpUrl = AiUrls.McpResource(HttpContext, _options),
                Configured = _options.IsConfigured,
                PatScopes = await _service.PatScopeOptions(),
            });
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ConnectionListResponse))]
        public async Task<IActionResult> List()
        {
            return Ok(await _service.List(CurrentUserId));
        }

        [Route("Revoke")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
        {
            try
            {
                await _service.Revoke(CurrentUserId, request.Id);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [Route("CreatePat")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(CreatePatResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreatePat([FromBody] CreatePatRequest request)
        {
            if (!_options.IsConfigured) return StatusCode(503, "AI connector ยังไม่ได้เปิดใช้งาน");
            try
            {
                return Ok(await _service.CreatePat(CurrentUserId, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, ex.Message);
            }
        }

        [Route("AccessLog")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(AccessLogResponse))]
        public async Task<IActionResult> AccessLog([FromBody] AccessLogRequest request)
        {
            return Ok(await _service.AccessLog(CurrentUserId, request));
        }
    }
}
