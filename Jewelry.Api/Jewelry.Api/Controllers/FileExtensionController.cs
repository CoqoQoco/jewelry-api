using jewelry.Model.Exceptions;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using Jewelry.Api.Controllers.Production;
using Jewelry.Api.Extension;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
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

        [Route("GetMoldImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetMoldImage(string imageName)
        {
            try
            {
                var response = _service.GetMoldImageBase64String(imageName);
                return Ok(response);
                //return Ok(new { Base64Image = response });
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetPlanMoldDesignImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetPlanMoldImage(string imageName)
        {
            try
            {
                var response = _service.GetPlanMoldDesignImageBase64String(imageName);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("GetPlanMoldResinImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetPlanMoldResinImage(string imageName)
        {
            try
            {
                var response = _service.GetPlanMoldResinImageBase64String(imageName);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetStockProductImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetImageBase64String(string imageName)
        {
            try
            {
                var response = _service.GetStockProductImageBase64String(imageName);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }


        [Route("GetImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult? GetImageBase64String(string imageName, string path)
        {
            try
            {
                var response = _service.GetImageBase64String(imageName, path);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
