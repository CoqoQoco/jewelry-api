using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Certificate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers.Certificate
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class CertificateController : ApiControllerBase
    {
        private readonly ILogger<CertificateController> _logger;
        private readonly ICertificateService _service;

        public CertificateController(ILogger<CertificateController> logger,
            ICertificateService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("UploadImage")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UploadImage([FromForm] jewelry.Model.Certificate.UploadImage.Request request)
        {
            try
            {
                var result = await _service.UploadImage(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error uploading certificate image");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading certificate image");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while uploading image" });
            }
        }

        [Route("CustomerBrand")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetCustomerBrand(string? customerCode)
        {
            try
            {
                var result = await _service.GetCustomerBrand(customerCode);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error getting customer brand: {CustomerCode}", customerCode);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error getting customer brand: {CustomerCode}", customerCode);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while getting customer brand" });
            }
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] jewelry.Model.Certificate.Create.Request request)
        {
            try
            {
                var result = await _service.Create(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error creating certificate for invoice: {InvoiceNumber}", request.InvoiceNumber);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating certificate for invoice: {InvoiceNumber}", request.InvoiceNumber);
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while creating certificate" });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> List([FromBody] jewelry.Model.Certificate.List.Request request)
        {
            try
            {
                var result = await _service.List(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error listing certificates");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing certificates");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing certificates" });
            }
        }
    }
}
