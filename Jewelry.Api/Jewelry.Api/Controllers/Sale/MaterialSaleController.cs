using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.MaterialSale;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class MaterialSaleController : ApiControllerBase
    {
        private readonly ILogger<MaterialSaleController> _logger;
        private readonly IMaterialSaleService _service;

        public MaterialSaleController(ILogger<MaterialSaleController> logger,
           IMaterialSaleService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("GenerateDocumentNumber")]
        public async Task<IActionResult> GenerateDocumentNumber()
        {
            try
            {
                var result = await _service.GenerateDocumentNumber();
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error generating material sale document number");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating material sale document number");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while generating document number" });
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(jewelry.Model.Sale.MaterialSale.Create.Request request)
        {
            try
            {
                var result = await _service.Create(request);
                return Ok(new { running = result.Running, documentNo = result.DocumentNo, message = "Material sale created successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error creating material sale");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating material sale");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while creating material sale" });
            }
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update(jewelry.Model.Sale.MaterialSale.Update.Request request)
        {
            try
            {
                var result = await _service.Update(request);
                return Ok(new { running = result, message = "Material sale updated successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error updating material sale");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating material sale");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while updating material sale" });
            }
        }

        [HttpPost("Get")]
        public async Task<IActionResult> Get(jewelry.Model.Sale.MaterialSale.Get.Request request)
        {
            try
            {
                var result = await _service.Get(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting material sale: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting material sale: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting material sale" });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult List([FromBody] jewelry.Model.Sale.MaterialSale.List.Request request)
        {
            try
            {
                var query = _service.List(request);
                var response = query.ToDataSourceResult(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing material sales");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing material sales");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing material sales" });
            }
        }

        [HttpPost("Confirm")]
        public async Task<IActionResult> Confirm(jewelry.Model.Sale.MaterialSale.Confirm.Request request)
        {
            try
            {
                var result = await _service.Confirm(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error confirming material sale: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error confirming material sale: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while confirming material sale" });
            }
        }

        [HttpPost("Cancel")]
        public async Task<IActionResult> Cancel(jewelry.Model.Sale.MaterialSale.Cancel.Request request)
        {
            try
            {
                var result = await _service.Cancel(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error cancelling material sale: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error cancelling material sale: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while cancelling material sale" });
            }
        }

        [HttpPost("Delete")]
        public async Task<IActionResult> Delete(jewelry.Model.Sale.MaterialSale.Delete.Request request)
        {
            try
            {
                var result = await _service.Delete(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error deleting material sale: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting material sale: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while deleting material sale" });
            }
        }
    }
}
