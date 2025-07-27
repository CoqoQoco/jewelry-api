using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanStatus.Transfer;
using Jewelry.Api.Extension;
using Jewelry.Service.Production.Plan;
using Jewelry.Service.ProductionPlan;
using Kendo.DynamicLinqCore;
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

        [Route("StatusDetailList")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Production.Plan.StatusDetailList.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult StatusDetailList([FromBody] jewelry.Model.Production.Plan.StatusDetailList.Request request)
        {
            try
            {
                var response = _planService.StatusDetailList(request.Search);
                return response.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("TransferList")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Production.Plan.TransferList.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult TransferList([FromBody] jewelry.Model.Production.Plan.TransferList.Request request)
        {
            try
            {
                var response = _planService.TransferList(request.Search);
                return response.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
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

        [Route("DailyPlan")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Production.Plan.DailyPlan.Response>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetDailyRemainByStatus([FromBody] jewelry.Model.Production.Plan.DailyPlan.Request request)
        {
            try
            {
                var response = await _planService.GetDailyReport(request.Search);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MonthlyReport")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Production.Plan.MonthlyReport.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetPlanSuccessMonthlyReport([FromBody] jewelry.Model.Production.Plan.MonthlyReport.Request request)
        {
            try
            {
                var response = await _planService.GetPlanSuccessMonthlyReport(request.Search);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
