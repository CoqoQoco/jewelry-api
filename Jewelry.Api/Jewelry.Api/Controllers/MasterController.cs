using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using jewelry.Model.Master.SearchMaster;
using jewelry.Model.ProductionPlan.ProductionPlanStatus;
using Jewelry.Api.Controllers.Production;
using Jewelry.Api.Extension;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Master;
using Jewelry.Service.ProductionPlan;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class MasterController : ApiControllerBase
    {
        private readonly ILogger<ProductionPlanController> _logger;
        private readonly IMasterService _service;

        public MasterController(ILogger<ProductionPlanController> logger,
            IMasterService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("MasterGold")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterGold()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterGold();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("MasterGoldSize")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterGoldSize()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterGoldSize();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("MasterGem")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterGem()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterGem();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("MasterGemShape")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterGemShape()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterGemShape();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("MasterProductType")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterProductType()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterProductType();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("MasterCustomerType")]
        [HttpGet]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult MasterCustomerType()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.MasterCustomerType();
                //var result = report.ToDataSource(request);

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }


        // ------ post to get master
        [Route("SearchMaster")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult SearchMaster([FromBody] SearchMasterModelRequest request)
        {
            try
            {
                var response = _service.SearchMaster(request.Search);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ListMaster")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ListMaster([FromBody] SearchMasterRequest request)
        {
            try
            {
                var response = _service.SearchMaster(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("UpdateMasterModel")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateMasterModel([FromBody] UpdateEditMasterModelRequest request)
        {
            try
            {
                var response = await _service.UpdateMasterModel(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("DeleteMasterModel")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteMasterModel([FromBody] DeleteMasterModelRequest request)
        {
            try
            {
                var response = await _service.DeleteMasterModel(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("CreateMasterModel")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateMasterModel([FromBody] CreateMasterModelRequest request)
        {
            try
            {
                var response = await _service.CreateMasterModel(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
