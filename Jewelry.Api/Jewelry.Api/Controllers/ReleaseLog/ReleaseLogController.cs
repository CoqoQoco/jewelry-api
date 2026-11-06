using jewelry.Model.Exceptions;
using jewelry.Model.ReleaseLog;
using Jewelry.Api.Extension;
using Jewelry.Service.ReleaseLog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers.ReleaseLog
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ReleaseLogController : ApiControllerBase
    {
        private readonly IReleaseLogService _service;

        public ReleaseLogController(
            IReleaseLogService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Create")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(long))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateReleaseLogRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var id = await _service.Create(request);
                return Ok(id);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Latest")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(LatestReleaseLogResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Latest([FromBody] LatestReleaseLogRequest request)
        {
            return Ok(await _service.Latest());
        }
    }
}
