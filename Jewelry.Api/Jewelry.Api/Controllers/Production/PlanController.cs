using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using Jewelry.Api.Extension;
using Jewelry.Service.Production.Plan;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Production
{
    [Route("Production/[controller]")]
    [ApiController]
    //[Authorize]
    public class PlanController : ApiControllerBase
    {
        private readonly ILogger<PlanController> _logger;
        private readonly IPlanService _planService;

        public PlanController(ILogger<PlanController> logger,
            IPlanService planService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _planService = planService;
        }

        [Route("Transfer")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Production.Plan.Transfer.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanTransfer([FromBody] jewelry.Model.Production.Plan.Transfer.Request request)
        {
            try
            {
                var response = await _planService.Transfer(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
