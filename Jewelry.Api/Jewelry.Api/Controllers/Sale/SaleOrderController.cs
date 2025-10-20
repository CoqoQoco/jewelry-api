using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using Jewelry.Api.Extension;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Jewelry.Service.Sale.SaleOrder;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleOrderController : ApiControllerBase
    {
        private readonly ILogger<SaleOrderController> _logger;
        private readonly ISaleOrderService _service;

        public SaleOrderController(ILogger<SaleOrderController> logger,
           ISaleOrderService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Upsert")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Upsert([FromBody] jewelry.Model.Sale.SaleOrder.Create.Request request)
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
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Sale.SaleOrder.Get.Response))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Sale.SaleOrder.Get.Request request)
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
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Sale.SaleOrder.List.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult List([FromBody] jewelry.Model.Sale.SaleOrder.List.Request request)
        {
            try
            {
                var query = _service.List(request);
                var response = query.ToDataSourceResult(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GenerateRunningNumber")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GenerateRunningNumber()
        {
            try
            {
                var runningNumber = await _service.GenerateRunningNumber();
                return Ok(runningNumber);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ConfirmStockItems")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Sale.SaleOrder.ConfirmStock.Response))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ConfirmStockItems([FromBody] jewelry.Model.Sale.SaleOrder.ConfirmStock.Request request)
        {
            try
            {
                var response = await _service.ConfirmStockItems(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UnconfirmStockItems")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Sale.SaleOrder.UnconfirmStock.Response))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UnconfirmStockItems([FromBody] jewelry.Model.Sale.SaleOrder.UnconfirmStock.Request request)
        {
            try
            {
                var response = await _service.UnconfirmStockItems(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}