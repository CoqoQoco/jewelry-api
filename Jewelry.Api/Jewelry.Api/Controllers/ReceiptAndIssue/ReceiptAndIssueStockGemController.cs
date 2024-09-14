using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Inbound;
using jewelry.Model.Receipt.Gem.List;
using jewelry.Model.Receipt.Gem.Outbound;
using jewelry.Model.Receipt.Gem.Picklist;
using jewelry.Model.Receipt.Gem.PickOff;
using jewelry.Model.Receipt.Gem.Return;
using jewelry.Model.Receipt.Gem.Scan;
using jewelry.Model.Stock.Mold.CheckOut;
using Jewelry.Api.Extension;
using Jewelry.Service.Receipt.Gem;
using Jewelry.Service.Stock;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Receipt
{
    [Route("/[controller]")]
    [ApiController]
    public class ReceiptAndIssueStockGemController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IReceiptAndIssueStockGemService _service;

        public ReceiptAndIssueStockGemController(ILogger<MoldController> logger,
           IReceiptAndIssueStockGemService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("CreateGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateGem([FromBody] CreateRequest request)
        {
            try
            {
                var response = await _service.CreateGem(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Scan")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(ScanResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Scan([FromBody] ScanRequest request)
        {
            try
            {
                var result = await _service.Scan(request);
                if (!result.Any())
                {
                    return BadRequest(new NotFoundResponse() { Message = "ไม่พบข้อมูล" });
                }

                var response = result.FirstOrDefault();
                if (response == null)
                {
                    return BadRequest(new NotFoundResponse() { Message = "ไม่พบข้อมูล" });
                }
                //if (response.Quantity <= 0)
                //{
                //    return BadRequest(new NotFoundResponse() { Message = "ข้อมูลไม่พร้อมใช้งาน [จำนวนคงคลังไม่เพียงพอ]" });
                //}

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }


        [Route("ListTransection")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<ListResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ListTransection([FromBody] ListRequest request)
        {
            try
            {
                var response = _service.ListTransection(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("InboundGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> InboundGem([FromBody] InboundRequest request)
        {
            try
            {
                var result = await _service.InboundGem(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("OutboundGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> OutboundGem([FromBody] OutboundRequest request)
        {
            try
            {
                var result = await _service.OutboundGem(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Picklist")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<PicklistResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult Picklist([FromBody] PicklistRequest request)
        {
            try
            {
                var response = _service.Picklist(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("PickOffGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PickOffGem([FromBody] PickOffRequest request)
        {
            try
            {
                var result = await _service.PickOffGem(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PickReturnGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(PickReturnResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PickReturnGem([FromBody] PickReturnRequest request)
        {
            try
            {
                var result = await _service.PickReturnGem(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
