using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Sale.StockBasket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockBasketController : ApiControllerBase
    {
        private readonly ILogger<StockBasketController> _logger;
        private readonly IStockBasketService _service;

        public StockBasketController(ILogger<StockBasketController> logger,
            IStockBasketService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Upsert")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Upsert([FromBody] jewelry.Model.Sale.StockBasket.Create.Request request)
        {
            try
            {
                var response = await _service.Upsert(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Sale.StockBasket.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Sale.StockBasket.Get.Request request)
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

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IList<jewelry.Model.Sale.StockBasket.List.Response>))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> List([FromBody] jewelry.Model.Sale.StockBasket.List.Request request)
        {
            try
            {
                var (data, total) = await _service.List(request);
                return Ok(new { Data = data, Total = total });
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GenerateNumber")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GenerateNumber()
        {
            try
            {
                var response = await _service.GenerateNumber();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("AddItems")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Sale.StockBasket.AddItems.Response))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddItems([FromBody] jewelry.Model.Sale.StockBasket.AddItems.Request request)
        {
            try
            {
                var response = await _service.AddItems(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("RemoveItem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> RemoveItem([FromBody] jewelry.Model.Sale.StockBasket.RemoveItem.Request request)
        {
            try
            {
                await _service.RemoveItem(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SubmitApproval")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SubmitApproval([FromBody] jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
        {
            try
            {
                await _service.SubmitApproval(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Approve")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Approve([FromBody] jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
        {
            try
            {
                await _service.Approve(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Checkout")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Checkout([FromBody] jewelry.Model.Sale.StockBasket.UpdateStatus.Request request)
        {
            try
            {
                await _service.Checkout(request);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
