using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.SaleOrderDeposit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleOrderDepositController : ApiControllerBase
    {
        private readonly ILogger<SaleOrderDepositController> _logger;
        private readonly ISaleOrderDepositService _service;

        public SaleOrderDepositController(ILogger<SaleOrderDepositController> logger,
           ISaleOrderDepositService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("Create")]
        [RequirePermission("sale:deposit")]
        public async Task<IActionResult> Create([FromForm] jewelry.Model.Sale.SaleOrderDeposit.Create.Request request)
        {
            try
            {
                var result = await _service.Create(request);
                return Ok(new { depositRunning = result, message = "Deposit created successfully" });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error creating deposit for sale order: {SoNumber}", request.SoNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating deposit for sale order: {SoNumber}", request.SoNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while creating deposit" });
            }
        }

        [HttpPost("List")]
        public async Task<IActionResult> List([FromBody] jewelry.Model.Sale.SaleOrderDeposit.List.Request request)
        {
            try
            {
                var result = await _service.List(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing deposits for sale order: {SoNumber}", request.SoNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing deposits for sale order: {SoNumber}", request.SoNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing deposits" });
            }
        }

        [HttpPost("Delete")]
        [RequirePermission("sale:deposit")]
        public async Task<IActionResult> Delete([FromBody] jewelry.Model.Sale.SaleOrderDeposit.Delete.Request request)
        {
            try
            {
                var result = await _service.Delete(request);
                return Ok(new { message = result });
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error deleting deposit: {Running}", request.Running);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting deposit: {Running}", request.Running);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while deleting deposit" });
            }
        }
    }
}
