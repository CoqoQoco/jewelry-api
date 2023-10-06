using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using jewelry.Model.ProductionPlan.ProductionPlanDelete;
using jewelry.Model.ProductionPlan.ProductionPlanTracking;
using jewelry.Model.ProductionPlan.ProductionPlanUpdate;
using Jewelry.Api.Extension;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Jewelry.Service.User;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class ProductionPlanController : ApiControllerBase
    {
        private readonly ILogger<ProductionPlanController> _logger;
        private readonly IProductionPlanService _IProductionPlanService;

        public ProductionPlanController(ILogger<ProductionPlanController> logger,
            IProductionPlanService IProductionPlanService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _IProductionPlanService = IProductionPlanService;
        }

        [Route("ProductionPlanCreate")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(ProductionPlanCreateResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanCreate([FromForm] ProductionPlanCreateRequest request)
        {
            try
            {
                var response = await _IProductionPlanService.ProductionPlanCreate(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ProductionPlanCreateImage")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(ProductionPlanCreateResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanCreateImage([FromForm] List<IFormFile> files, [FromForm] string wo, [FromForm] int woNumber)
        {
            try
            {
                var response = await _IProductionPlanService.ProductionPlanCreateImage(files, wo, woNumber);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ProductionPlanSearch")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<TbtProductionPlan>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult ProductionPlanSearch([FromBody] ProductionPlanTrackingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var report = _IProductionPlanService.ProductionPlanSearch(request.Search);
                var result = report.ToDataSource(request);

                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ProductionPlanGet")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(TbtProductionPlan))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult ProductionPlanGet(int id)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var report = _IProductionPlanService.ProductionPlanGet(id);
                return Ok(report);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ProductionPlanMateriaGet")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<TbtProductionPlan>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult ProductionPlanMaterialSearch([FromBody] ProductionPlanTrackingMaterialRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var report = _IProductionPlanService.ProductionPlanMateriaGet(request);
                return Ok(report);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ProductionPlanUpdateStatus")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanUpdateStatus([FromBody] ProductionPlanUpdateStatusRequest request)
        {
            try
            {
                var response = await _IProductionPlanService.ProductionPlanUpdateStatus(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetProductionPlanStatus")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<TbmProductionPlanStatus>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult GetProductionPlanStatus()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _IProductionPlanService.GetProductionPlanStatus();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ProductionPlanUpdateHeader")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanUpdateHeader([FromBody] ProductionPlanUpdateHeaderRequest request)
        {
            try
            {
                var response = await _IProductionPlanService.ProductionPlanUpdateHeader(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ProductionPlanDeleteMaterial")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanDeleteMaterial([FromBody] ProductionPlanMaterialDeleteRequest request)
        {
            try
            {
                var response = await _IProductionPlanService.ProductionPlanDeleteMaterial(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

    }
}
