using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.BillingNote;
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
    public class BillingNoteController : ApiControllerBase
    {
        private readonly ILogger<BillingNoteController> _logger;
        private readonly IBillingNoteService _service;

        public BillingNoteController(ILogger<BillingNoteController> logger,
           IBillingNoteService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet("AvailableInvoices")]
        public async Task<IActionResult> AvailableInvoices([FromQuery] string customerCode)
        {
            try
            {
                var result = await _service.AvailableInvoices(new jewelry.Model.Sale.BillingNote.AvailableInvoices.Request
                {
                    CustomerCode = customerCode
                });
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting available invoices for customer: {CustomerCode}", customerCode);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting available invoices for customer: {CustomerCode}", customerCode);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting available invoices" });
            }
        }

        [HttpGet("AvailableCustomers")]
        public async Task<IActionResult> AvailableCustomers()
        {
            try
            {
                var result = await _service.AvailableCustomers();
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting available customers for billing note");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting available customers for billing note");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting available customers" });
            }
        }

        [HttpPost("PreviewProducts")]
        public async Task<IActionResult> PreviewProducts(jewelry.Model.Sale.BillingNote.PreviewProducts.Request request)
        {
            try
            {
                var result = await _service.PreviewProducts(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error previewing billing note products");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error previewing billing note products");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while previewing products" });
            }
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(jewelry.Model.Sale.BillingNote.Create.Request request)
        {
            try
            {
                var result = await _service.Create(request);
                return Ok(new { running = result, message = "Billing note created successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error creating billing note");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating billing note");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while creating billing note" });
            }
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update(jewelry.Model.Sale.BillingNote.Update.Request request)
        {
            try
            {
                var result = await _service.Update(request);
                return Ok(new { running = result, message = "Billing note updated successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error updating billing note");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating billing note");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while updating billing note" });
            }
        }

        [HttpPost("Get")]
        public async Task<IActionResult> Get(jewelry.Model.Sale.BillingNote.Get.Request request)
        {
            try
            {
                var result = await _service.Get(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting billing note: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting billing note: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting billing note" });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult List([FromBody] jewelry.Model.Sale.BillingNote.List.Request request)
        {
            try
            {
                var query = _service.List(request);
                var response = query.ToDataSourceResult(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing billing notes");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing billing notes");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing billing notes" });
            }
        }

        [HttpPost("Delete")]
        public async Task<IActionResult> Delete(jewelry.Model.Sale.BillingNote.Delete.Request request)
        {
            try
            {
                var result = await _service.Delete(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error deleting billing note: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting billing note: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while deleting billing note" });
            }
        }
    }
}
