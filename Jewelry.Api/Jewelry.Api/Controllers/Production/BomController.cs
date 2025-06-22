using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using Jewelry.Api.Extension;
using Jewelry.Service.Production.PlanBOM;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using Kendo.DynamicLinqCore;

namespace Jewelry.Api.Controllers.Production
{

    [Route("Production/[controller]")]
    [ApiController]
    [Authorize]
    public class BomController : ApiControllerBase
    {

        private readonly ILogger<BomController> _logger;
        private readonly IPlanBOMService _IProductionBomService;

        public BomController(ILogger<BomController> logger,
            IPlanBOMService IProductionBomService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _IProductionBomService = IProductionBomService;
        }

        [Route("Transaction")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Production.PlanBOM.NewGet.Response))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanCreate(int id)
        {
            try
            {
                var response = await _IProductionBomService.GetTransactionBOM(id);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Save")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Base.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SaveBOM([FromBody] jewelry.Model.Production.PlanBOM.Save.Request request)
        {
            try
            {
                var response = await _IProductionBomService.SavePlanBom(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Production.PlanBOM.NewGet.BOM>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetBOMByProductionPlanId(int id)
        {
            try
            {
                var response = await _IProductionBomService.GetPlanBom(id);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IQueryable<jewelry.Model.Production.PlanBOM.List.Response>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public DataSourceResult ListBOM([FromBody] jewelry.Model.Production.PlanBOM.List.Request request)
        {
            try
            {
                var bomList = _IProductionBomService.ListBom(request.Search ?? new jewelry.Model.Production.PlanBOM.List.Criteria());
                return bomList.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
    }
}
