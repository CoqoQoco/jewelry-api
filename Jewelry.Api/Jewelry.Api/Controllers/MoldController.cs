using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using jewelry.Model.Mold;
using jewelry.Model.Mold.PlanCasting;
using jewelry.Model.Mold.PlanCastingSilver;
using jewelry.Model.Mold.PlanCutting;
using jewelry.Model.Mold.PlanDesign;
using jewelry.Model.Mold.PlanGet;
using jewelry.Model.Mold.PlanList;
using jewelry.Model.Mold.PlanResin;
using jewelry.Model.Mold.PlanStore;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanGet;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using Jewelry.Api.Extension;
using Jewelry.Service.Mold;
using Jewelry.Service.ProductionPlan;
using Jewelry.Service.Stock;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class MoldController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IMoldService _service;
        private readonly IMoldPlanService _servicePlan;

        public MoldController(ILogger<MoldController> logger,
            IMoldService service,
            IMoldPlanService servicePlan,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
            _servicePlan = servicePlan;
        }

        [Route("CreateMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateMold([FromForm] CreateMoldRequest request)
        {
            try
            {
                var response = await _service.CreateMold(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UpdateMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateMold([FromForm] UpdateMoldRequest request)
        {
            try
            {
                var response = await _service.UpdateMold(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult SearchMold([FromBody] SearchMoldRequest request)
        {
            try
            {
                var response = _service.SearchMold(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        //new mold plan

        [Route("PlanList")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<PlanListReponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult PlanList([FromBody] PlanListRequest request)
        {
            try
            {
                var response = _servicePlan.PlanList(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("PlanGet")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(PlanGetResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult PlanGet(int id)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var report = _servicePlan.PlanGet(id);
                return Ok(report);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("PlanDesign")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanDesign([FromForm] PlanDesignRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanDesign(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PlanResin")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanResin([FromForm] PlanResinRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanResin(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PlanCastingSilver")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanCastingSilver([FromForm] PlanCastingSilverRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanCastingSilver(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PlanCasting")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanCasting([FromForm] PlanCastingRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanCasting(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PlanCutting")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanCutting([FromForm] PlanCuttingRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanCutting(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PlanStore")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PlanStore([FromForm] PlanStoreRequest request)
        {
            try
            {
                var response = await _servicePlan.PlanStore(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
