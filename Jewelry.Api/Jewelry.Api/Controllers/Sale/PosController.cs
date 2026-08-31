using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class PosController : ApiControllerBase
    {
        private readonly ILogger<PosController> _logger;
        private readonly IPosCheckoutService _service;

        public PosController(ILogger<PosController> logger,
           IPosCheckoutService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Checkout")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.Pos.Checkout.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Checkout([FromBody] jewelry.Model.Sale.Pos.Checkout.Request request)
        {
            try
            {
                var response = await _service.Checkout(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                _logger.LogError(ex, "Error on POS checkout");
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
