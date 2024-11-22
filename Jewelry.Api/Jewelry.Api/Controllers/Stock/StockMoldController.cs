using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using jewelry.Model.Stock.Mold.CheckOut;
using jewelry.Model.Stock.Mold.CheckOutList;
using jewelry.Model.Stock.Mold.Return;
using Jewelry.Api.Extension;
using Jewelry.Service.Mold;
using Jewelry.Service.Stock;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockMoldController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IStockMoldService _service;

        public StockMoldController(ILogger<MoldController> logger,
           IStockMoldService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("SearchCheckOutList")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<CheckOutListResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult SearchCheckOutList([FromBody] CheckOutListRequest request)
        {
            try
            {
                var response = _service.SearchCheckOutList(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("CheckOutMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CheckOutMold([FromBody] CheckOutRequest request)
        {
            try
            {
                var response = await _service.CheckOutMold(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("ReturnMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ReturnMold([FromBody] ReturnRequest request)
        {
            try
            {
                var response = await _service.ReturnMold(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
