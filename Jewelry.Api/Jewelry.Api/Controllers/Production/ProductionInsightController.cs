using Jewelry.Api.Extension;
using Jewelry.Service.Production.Insight;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading.Tasks;
using Wip = jewelry.Model.Production.Insight.Wip;
using DueRiskPlans = jewelry.Model.Production.Insight.DueRiskPlans;
using StalePlans = jewelry.Model.Report.Executive.StalePlans;

namespace Jewelry.Api.Controllers.Production
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductionInsightController : ApiControllerBase
    {
        private readonly IProductionInsightService _service;

        public ProductionInsightController(IProductionInsightService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Wip")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Wip.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Wip([FromBody] Wip.Request request)
        {
            try
            {
                var response = await _service.Wip(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StalePlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StalePlans([FromBody] StalePlans.Request request)
        {
            try
            {
                var response = await _service.StalePlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DueRiskPlans")]
        [HttpPost]
        [RequirePermission("executive:view", "production:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DueRiskPlans([FromBody] DueRiskPlans.Request request)
        {
            try
            {
                var response = await _service.DueRiskPlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
