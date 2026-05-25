using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Movement;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    }
}
