using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem;
using Jewelry.Api.Extension;
using Jewelry.Service.Customer;
using Jewelry.Service.Stock;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class GemStockController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IStockGemService _service;

        public GemStockController(ILogger<MoldController> logger,
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
    }
}
