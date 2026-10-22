using Jewelry.Api.Extension;
using Jewelry.Service.Sale.Insight;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading.Tasks;
using Overview = jewelry.Model.Sale.Insight.Overview;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesInsightController : ApiControllerBase
    {
        private readonly ISalesInsightService _service;

        public SalesInsightController(ISalesInsightService service,
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
                var response = await _service.Overview(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
