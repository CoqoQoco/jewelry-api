using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Option;
using jewelry.Model.Stock.Gem.Price;
using jewelry.Model.Stock.Gem.PriceEdit;
using jewelry.Model.Stock.Gem.Search;
using jewelry.Model.Stock.Mold.Return;
using Jewelry.Api.Extension;
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
    public class StockGemController : ApiControllerBase
    {
        private readonly ILogger<StockGemController> _logger;
        private readonly IStockGemService _service;

        public StockGemController(ILogger<StockGemController> logger,
            IStockGemService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Search")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(List<SearchGemResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult Search([FromBody] SearchGemRequest request)
        {
            try
            {
                var report = _service.SearchGem(request.Search);
                return Ok(report);
                //return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                //return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchData")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<SearchGemResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult SearchData([FromBody] SearchGemRequest request)
        {
            try
            {
                var report = _service.SearchGemData(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
        [Route("GroupGemData")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(OptionResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GroupGemData([FromBody] OptionRequest request)
        {
            try
            {
                var response =  _service.GroupGemData(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Price")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(OptionResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Price([FromBody] PriceEditRequest request)
        {
            try
            {
                var response = _service.Price(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
        [Route("PriceHistory")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<SearchGemResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult SearchData([FromBody] PriceRequest request)
        {
            try
            {
                var report = _service.PriceHistory(request.Search);
                return report.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
    }
}
