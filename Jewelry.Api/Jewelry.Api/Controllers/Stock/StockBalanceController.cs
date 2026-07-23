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

        [HttpPost("ByStockNumbers")]
        public async Task<IActionResult> ByStockNumbers([FromBody] jewelry.Model.Stock.Balance.ByStockNumbers.Request request, CancellationToken ct)
        {
            var result = await _service.ListByStockNumbersAsync(request.StockNumbers, ct);
            return Ok(result);
        }

        [Route("Summary")]
        [HttpPost]
        public async Task<IActionResult> Summary([FromBody] jewelry.Model.Stock.Balance.Summary.Request request)
        {
            var result = await _service.GetSummary(request);
            return Ok(result);
        }
    }
}
