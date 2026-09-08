using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.PublicProduct;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System;
using System.Net;

namespace Jewelry.Api.Controllers.Public
{
    [Route("/[controller]")]
    [ApiController]
    [EnableRateLimiting("public")]
    public class PublicProductController : ApiControllerBase
    {
        private readonly ILogger<PublicProductController> _logger;
        private readonly IPublicProductService _service;

        public PublicProductController(ILogger<PublicProductController> logger,
            IPublicProductService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.PublicProduct.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public IActionResult Get([FromBody] jewelry.Model.PublicProduct.Get.Request request)
        {
            try
            {
                var response = _service.Get(request.Token);
                return Ok(response);
            }
            catch (HandleException)
            {
                return NotFound(new NotFoundResponse() { Message = "not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PublicProductController.Get failed");
                return NotFound(new NotFoundResponse() { Message = "not found" });
            }
        }
    }
}
