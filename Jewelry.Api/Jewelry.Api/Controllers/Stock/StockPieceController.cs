using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Piece;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockPieceController : ApiControllerBase
    {
        private readonly ILogger<StockPieceController> _logger;
        private readonly IStockPieceService _service;

        public StockPieceController(ILogger<StockPieceController> logger,
            IStockPieceService service,
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
    }
}
