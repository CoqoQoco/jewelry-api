using Jewelry.Api.Extension;
using Jewelry.Service.Stock.Reconciliation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockReconciliationController : ApiControllerBase
    {
        private readonly ILogger<StockReconciliationController> _logger;
        private readonly IStockReconciliationService _service;

        public StockReconciliationController(ILogger<StockReconciliationController> logger,
            IStockReconciliationService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("CheckDrift")]
        [HttpPost]
        public async Task<IActionResult> CheckDrift(CancellationToken ct)
        {
            var report = await _service.CheckDriftAsync(ct);
            return Ok(report);
        }

        [Route("RebuildBalance")]
        [HttpPost]
        public async Task<IActionResult> RebuildBalance(CancellationToken ct)
        {
            var rebuiltCount = await _service.RebuildBalanceFromPiecesAsync(ct);
            return Ok(new { rebuiltCount });
        }
    }
}
