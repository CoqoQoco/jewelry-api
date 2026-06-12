using jewelry.Model.Exceptions;
using jewelry.Model.Sale.SaleDocumentCatalog;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.SaleDocumentCatalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleDocumentCatalogController : ApiControllerBase
    {
        private readonly ISaleDocumentCatalogService _service;

        public SaleDocumentCatalogController(
            ISaleDocumentCatalogService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Save")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(long))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Save([FromBody] SaveRequest request)
        {
            try
            {
                var id = await _service.Save(request);
                return Ok(id);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get/{id}")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(GetResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get(long id)
        {
            try
            {
                var result = await _service.Get(id);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<ListResponse>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> List([FromBody] ListRequest request)
        {
            var result = await _service.List(request);
            return Ok(result);
        }

        [Route("Delete/{id}")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _service.Delete(id);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UploadImage")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadImageResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            try
            {
                var result = await _service.UploadImage(file);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetImage")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetImage([FromQuery] string blobPath)
        {
            try
            {
                var (stream, contentType) = await _service.GetImage(blobPath);
                return File(stream, contentType);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
