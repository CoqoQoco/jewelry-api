using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Movement.Move;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Movement;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using SearchModel = jewelry.Model.Stock.Movement.Search;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockMovementController : ApiControllerBase
    {
        private readonly ILogger<StockMovementController> _logger;
        private readonly IStockMovementService _service;

        public StockMovementController(ILogger<StockMovementController> logger,
            IStockMovementService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("List")]
        [HttpPost]
        public DataSourceResult List([FromBody] DataSourceRequest request)
        {
            var response = _service.List();
            return response.ToDataSourceResult(request);
        }

        [Route("Move")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Move([FromBody] Request request)
        {
            try
            {
                var response = await _service.MoveLocation(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Search")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IQueryable<SearchModel.Response>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public DataSourceResult Search([FromBody] SearchModel.Request request)
        {
            var response = _service.Search(request);
            return response.ToDataSource(request);
        }
    }
}
