using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Balance;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockBalanceController : ApiControllerBase
    {
        private readonly ILogger<StockBalanceController> _logger;
        private readonly IStockBalanceService _service;

        public StockBalanceController(ILogger<StockBalanceController> logger,
            IStockBalanceService service,
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
