using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using Jewelry.Api.Extension;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class FileExtensionController : ApiControllerBase
    {
        private readonly ILogger<ProductionPlanController> _logger;
        private readonly IFileExtension _service;

        public FileExtensionController(IFileExtension service, ILogger<ProductionPlanController> logger,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("GetPlanImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetPlanImage(string imageName)
        {
            try
            {
                var response = _service.GetPlanImageBase64String(imageName);
                return Ok(response);
                //return Ok(new { Base64Image = response });
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
