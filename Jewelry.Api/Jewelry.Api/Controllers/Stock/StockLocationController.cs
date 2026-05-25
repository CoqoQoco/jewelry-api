using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Location;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockLocationController : ApiControllerBase
    {
        private readonly ILogger<StockLocationController> _logger;
        private readonly IStockLocationService _service;

        public StockLocationController(ILogger<StockLocationController> logger,
            IStockLocationService service,
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
