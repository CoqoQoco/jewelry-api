using Jewelry.Api.Extension;
using Jewelry.Service.Material.Insight;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading.Tasks;
using Min = jewelry.Model.Material.Insight.Min;
using Overview = jewelry.Model.Material.Insight.Overview;
using Policy = jewelry.Model.Material.Insight.Policy;

namespace Jewelry.Api.Controllers.Material
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class MaterialInsightController : ApiControllerBase
    {
        private readonly IMaterialInsightService _service;

        public MaterialInsightController(IMaterialInsightService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Overview")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Overview.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Overview([FromBody] Overview.Request request)
        {
            try
            {
                return Ok(await _service.Overview(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Policy/Get")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Policy.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PolicyGet([FromBody] Policy.GetRequest request)
        {
            try
            {
                return Ok(await _service.GetPolicy(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Policy/Save")]
        [HttpPost]
        [RequirePermission("material:manage-min")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Policy.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PolicySave([FromBody] Policy.SaveRequest request)
        {
            try
            {
                return Ok(await _service.SavePolicy(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Min/Save")]
        [HttpPost]
        [RequirePermission("material:manage-min")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Min.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MinSave([FromBody] Min.SaveRequest request)
        {
            try
            {
                return Ok(await _service.SaveMin(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Min/Delete")]
        [HttpPost]
        [RequirePermission("material:manage-min")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Min.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MinDelete([FromBody] Min.DeleteRequest request)
        {
            try
            {
                return Ok(await _service.DeleteMin(request));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
