using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlanCost.GoldCostCreate;
using jewelry.Model.ProductionPlanCost.GoldCostList;
using jewelry.Model.ProductionPlanCost.GoldCostReport;
using jewelry.Model.ProductionPlanCost.GoldCostUpdate;
using Jewelry.Api.Extension;
using Jewelry.Service.ProductionPlan;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class ProductionPlanCostController : ApiControllerBase
    {
        private readonly ILogger<ProductionPlanController> _logger;
        private readonly IProductionPlanCostService _IProductionPlanCostService;

        public ProductionPlanCostController(ILogger<ProductionPlanController> logger,
            IProductionPlanCostService IProductionPlanService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _IProductionPlanCostService = IProductionPlanService;
        }


        [Route("ListGoldCost")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<GoldCostListResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ListGoldCost([FromBody] GoldCostListRequest request)
        {
            try
            {
                var report = _IProductionPlanCostService.ListGoldCost(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("Report")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<GoldCostListResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult Report([FromBody] GoldCostListRequest request)
        {
            try
            {
                var report = _IProductionPlanCostService.Report(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("SummeryReport")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(GoldCostSummeryResponse))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult SummeryReport([FromBody] GoldCostListRequest request)
        {
            try
            {
                var report = _IProductionPlanCostService.SummeryReport(request.Search);
                return Ok(report);

            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("CreateGoldCost")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateGoldCost([FromBody] GoldCostCreateRequest request)
        {
            try
            {
                var response = await _IProductionPlanCostService.CreateGoldCost(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("UpdateGoldCost")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateGoldCost([FromBody] GoldCostUpdateRequest request)
        {
            try
            {
                var response = await _IProductionPlanCostService.UpdateGoldCost(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
