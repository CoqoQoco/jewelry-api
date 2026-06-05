using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Catalog
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class CatalogController : ApiControllerBase
    {
        private readonly ILogger<CatalogController> _logger;
        private readonly ICatalogService _service;

        public CatalogController(ILogger<CatalogController> logger,
            ICatalogService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Catalog.List.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult List([FromBody] jewelry.Model.Catalog.List.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.List(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Catalog.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult Get([FromBody] jewelry.Model.Catalog.Get.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.Get(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] jewelry.Model.Catalog.Create.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Create(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Update")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update([FromBody] jewelry.Model.Catalog.Update.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Update(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Delete")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete([FromBody] jewelry.Model.Catalog.Delete.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Delete(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("AddProducts")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddProducts([FromBody] jewelry.Model.Catalog.AddProducts.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.AddProducts(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("RemoveProduct")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RemoveProduct([FromBody] jewelry.Model.Catalog.RemoveProduct.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.RemoveProduct(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
