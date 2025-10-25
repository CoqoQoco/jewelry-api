using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using Jewelry.Api.Extension;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Jewelry.Service.Sale.Invoice;
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
    public class InvoiceController : ApiControllerBase
    {
        private readonly ILogger<InvoiceController> _logger;
        private readonly IInvoiceService _service;

        public InvoiceController(ILogger<InvoiceController> logger,
           IInvoiceService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(jewelry.Model.Sale.Invoice.Create.Request request)
        {
            try
            {
                var result = await _service.Create(request);
                return Ok(new { invoiceNumber = result, message = "Invoice created successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating invoice");
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = "An error occurred while creating invoice" });
            }
        }

        [HttpPost("Get")]
        public async Task<IActionResult> Get(jewelry.Model.Sale.Invoice.Get.Request request)
        {
            try
            {
                var result = await _service.Get(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting invoice: {InvoiceNumber}", request.InvoiceNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting invoice: {InvoiceNumber}", request.InvoiceNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = "An error occurred while getting invoice" });
            }
        }

        [HttpPost("List")]
        public IActionResult List(jewelry.Model.Sale.Invoice.List.Request request)
        {
            try
            {
                var query = _service.List(request);
                
                var result = query.Skip(request.Skip)
                                 .Take(request.Take)
                                 .ToList();
                
                var total = query.Count();

                return Ok(new 
                { 
                    data = result, 
                    total = total,
                    skip = request.Skip,
                    take = request.Take
                });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing invoices");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing invoices");
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = "An error occurred while listing invoices" });
            }
        }

        [HttpPost("Delete")]
        public async Task<IActionResult> Delete(jewelry.Model.Sale.Invoice.Delete.Request request)
        {
            try
            {
                var result = await _service.Delete(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error deleting invoice: {InvoiceNumber}", request.InvoiceNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting invoice: {InvoiceNumber}", request.InvoiceNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError, 
                    new { message = "An error occurred while deleting invoice" });
            }
        }

        [HttpGet("GenerateInvoiceNumber")]
        public async Task<IActionResult> GenerateInvoiceNumber()
        {
            try
            {
                var result = await _service.GenerateInvoiceNumber();
                return Ok(new { invoiceNumber = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while generating invoice number" });
            }
        }

        // Invoice Version Endpoints
        [HttpPost("Version/Upsert")]
        public async Task<IActionResult> UpsertVersion(jewelry.Model.Sale.InvoiceVersion.Upsert.Request request)
        {
            try
            {
                var result = await _service.UpsertVersion(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error upserting invoice version for invoice: {InvoiceNumber}", request.InvoiceNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error upserting invoice version for invoice: {InvoiceNumber}", request.InvoiceNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while upserting invoice version" });
            }
        }

        [HttpPost("Version/Get")]
        public async Task<IActionResult> GetVersion(jewelry.Model.Sale.InvoiceVersion.Get.Request request)
        {
            try
            {
                var result = await _service.GetVersion(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting invoice version: {VersionNumber}", request.VersionNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting invoice version: {VersionNumber}", request.VersionNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting invoice version" });
            }
        }

        [HttpPost("Version/List")]
        public IActionResult ListVersions(jewelry.Model.Sale.InvoiceVersion.List.Request request)
        {
            try
            {
                var query = _service.ListVersions(request);
                var result = query.ToList();
                var total = result.Count;

                return Ok(new
                {
                    data = result,
                    total = total
                });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing invoice versions");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing invoice versions");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing invoice versions" });
            }
        }
    }
}