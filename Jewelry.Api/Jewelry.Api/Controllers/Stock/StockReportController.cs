using Jewelry.Api.Extension;
using Jewelry.Service.Stock.StockReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockReportController : ApiControllerBase
    {
        private readonly IStockReportService _service;

        public StockReportController(IStockReportService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Summary")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.Summary.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Summary([FromBody] jewelry.Model.Stock.StockReport.Summary.Request request)
        {
            try
            {
                var response = await _service.Summary(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("ProductGroup")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.ProductGroup.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductGroup([FromBody] jewelry.Model.Stock.StockReport.ProductGroup.Request request)
        {
            try
            {
                var response = await _service.ProductGroup(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Aging")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.Aging.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Aging([FromBody] jewelry.Model.Stock.StockReport.Aging.Request request)
        {
            try
            {
                var response = await _service.Aging(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("AgingItems")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.AgingItems.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AgingItems([FromBody] jewelry.Model.Stock.StockReport.AgingItems.Request request)
        {
            try
            {
                var response = await _service.AgingItems(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("ProductionBalance")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.ProductionBalance.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionBalance([FromBody] jewelry.Model.Stock.StockReport.ProductionBalance.Request request)
        {
            try
            {
                var response = await _service.ProductionBalance(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("DesignAlerts")]
        [HttpPost]
        [RequirePermission("stock-product:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Stock.StockReport.DesignAlerts.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DesignAlerts([FromBody] jewelry.Model.Stock.StockReport.DesignAlerts.Request request)
        {
            try
            {
                var response = await _service.DesignAlerts(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
