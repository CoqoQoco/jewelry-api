using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock.StockProductGallery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockProductGalleryController : ApiControllerBase
    {
        private readonly IStockProductGalleryService _service;

        public StockProductGalleryController(
            IStockProductGalleryService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockProductGallery.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Stock.StockProductGallery.Get.Request request)
        {
            try
            {
                var response = await _service.Get(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Upload")]
        [HttpPost]
        [RequirePermission("stock-product-gr-image:create")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockProductGallery.Item))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Upload([FromForm] jewelry.Model.Stock.StockProductGallery.Upload.Request request)
        {
            try
            {
                var response = await _service.Upload(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Reorder")]
        [HttpPost]
        [RequirePermission("stock-product-gr-image:create")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Reorder([FromBody] jewelry.Model.Stock.StockProductGallery.Reorder.Request request)
        {
            try
            {
                await _service.Reorder(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Delete")]
        [HttpPost]
        [RequirePermission("stock-product-gr-image:create")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete([FromBody] jewelry.Model.Stock.StockProductGallery.Delete.Request request)
        {
            try
            {
                await _service.Delete(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MissingList")]
        [HttpPost]
        [RequirePermission("stock-product-gr-image:create")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockProductGallery.MissingList.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MissingList([FromBody] jewelry.Model.Stock.StockProductGallery.MissingList.Request request)
        {
            var response = await _service.MissingList(request);
            return Ok(response);
        }
    }
}
