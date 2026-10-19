using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Rfid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockRfidController : ApiControllerBase
    {
        private readonly IStockRfidService _service;

        public StockRfidController(
            IStockRfidService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Bind")]
        [HttpPost]
        [RequirePermission("stock-rfid:bind")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.Bind.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Bind([FromBody] jewelry.Model.Stock.Rfid.Bind.Request request)
        {
            try
            {
                var response = await _service.Bind(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Unbind")]
        [HttpPost]
        [RequirePermission("stock-rfid:bind")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.Unbind.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Unbind([FromBody] jewelry.Model.Stock.Rfid.Unbind.Request request)
        {
            try
            {
                var response = await _service.Unbind(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetByEpc")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.GetByEpc.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetByEpc([FromBody] jewelry.Model.Stock.Rfid.GetByEpc.Request request)
        {
            try
            {
                var response = await _service.GetByEpc(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetByEpcs")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.GetByEpcs.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetByEpcs([FromBody] jewelry.Model.Stock.Rfid.GetByEpcs.Request request)
        {
            try
            {
                var response = await _service.GetByEpcs(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("GetByStock")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.GetByStock.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetByStock([FromBody] jewelry.Model.Stock.Rfid.GetByStock.Request request)
        {
            try
            {
                var response = await _service.GetByStock(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListReceipts")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.ListReceipts.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ListReceipts([FromBody] jewelry.Model.Stock.Rfid.ListReceipts.Request request)
        {
            try
            {
                var response = await _service.ListReceipts(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListByReceipt")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.ListByReceipt.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ListByReceipt([FromBody] jewelry.Model.Stock.Rfid.ListByReceipt.Request request)
        {
            try
            {
                var response = await _service.ListByReceipt(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListUnbound")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.ListUnbound.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ListUnbound([FromBody] jewelry.Model.Stock.Rfid.ListUnbound.Request request)
        {
            try
            {
                var response = await _service.ListUnbound(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListRecent")]
        [HttpPost]
        [RequirePermission("stock-rfid:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.Rfid.ListRecent.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ListRecent([FromBody] jewelry.Model.Stock.Rfid.ListRecent.Request request)
        {
            try
            {
                var response = await _service.ListRecent(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
