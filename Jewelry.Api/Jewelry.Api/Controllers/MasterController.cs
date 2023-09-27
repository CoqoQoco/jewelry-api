using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using Jewelry.Api.Extension;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Master;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
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
    }
}
