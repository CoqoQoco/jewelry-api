using jewelry.Model.AiConnector;
using Jewelry.Api.Extension;
using Jewelry.Service.AiConnector.Auth;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.AiConnector
{
    // หน้า /setting/ai-admin — Dev คุมสิทธิ์ข้อมูล AI รายคน / ระงับ / ดูประวัติทุกคน
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    [RequirePermission("ai:admin")]
    public class AiAdminController : ApiControllerBase
    {
        private readonly IAiAdminService _service;

        public AiAdminController(IAiAdminService service, IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Users")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(AiAdminUserListResponse))]
        public async Task<IActionResult> Users()
        {
            return Ok(await _service.Users());
        }

        [Route("Connections")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ConnectionListResponse))]
        public async Task<IActionResult> Connections([FromBody] AiAdminConnectionsRequest request)
        {
            return Ok(await _service.Connections(request.UserId));
        }

        [Route("RevokeConnection")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public Task<IActionResult> RevokeConnection([FromBody] AiAdminRevokeRequest request) =>
            Run(() => _service.RevokeConnection(request.TokenId));

        [Route("SetGrant")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public Task<IActionResult> SetGrant([FromBody] AiAdminSetGrantRequest request) =>
            Run(() => _service.SetGrant(request));

        [Route("Suspend")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        public Task<IActionResult> Suspend([FromBody] AiAdminSuspendRequest request) =>
            Run(() => _service.Suspend(request));

        [Route("Lift")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public Task<IActionResult> Lift([FromBody] AiAdminLiftRequest request) =>
            Run(() => _service.Lift(request));

        [Route("AccessLog")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        public async Task<IActionResult> AccessLog([FromBody] AiAdminAccessLogRequest request)
        {
            return Ok(await _service.AccessLog(request));
        }

        [Route("Suspensions")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        public async Task<IActionResult> Suspensions([FromBody] AiAdminSuspensionsRequest request)
        {
            return Ok(await _service.Suspensions(request));
        }

        private async Task<IActionResult> Run(Func<Task> action)
        {
            try
            {
                await action();
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (AiAdminConflictException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}
