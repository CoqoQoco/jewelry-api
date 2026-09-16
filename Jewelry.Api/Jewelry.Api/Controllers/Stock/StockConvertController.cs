using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock.StockConvert;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockConvertController : ApiControllerBase
    {
        private readonly ILogger<StockConvertController> _logger;
        private readonly IStockConvertService _service;

        public StockConvertController(ILogger<StockConvertController> logger,
            IStockConvertService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Create")]
        [HttpPost]
        [RequirePermission("stock:convert")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockConvert.Create.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] jewelry.Model.Stock.StockConvert.Create.Request request)
        {
            try
            {
                var response = await _service.Create(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Complete")]
        [HttpPost]
        [RequirePermission("stock:convert")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockConvert.Complete.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Complete([FromBody] jewelry.Model.Stock.StockConvert.Complete.Request request)
        {
            try
            {
                var response = await _service.Complete(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Cancel")]
        [HttpPost]
        [RequirePermission("stock:convert")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockConvert.Cancel.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Cancel([FromBody] jewelry.Model.Stock.StockConvert.Cancel.Request request)
        {
            try
            {
                var response = await _service.Cancel(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> List([FromBody] jewelry.Model.Stock.StockConvert.List.Request request)
        {
            try
            {
                var response = await _service.List(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockConvert.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Stock.StockConvert.Get.Request request)
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

        [Route("PendingForSaleOrder")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Response>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PendingForSaleOrder([FromBody] jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Request request)
        {
            try
            {
                var response = await _service.PendingForSaleOrder(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
