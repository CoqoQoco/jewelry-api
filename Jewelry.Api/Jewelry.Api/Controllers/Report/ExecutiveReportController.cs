using Jewelry.Api.Extension;
using Jewelry.Service.Report.Executive;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Report
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ExecutiveReportController : ApiControllerBase
    {
        private readonly IExecutiveReportService _service;

        public ExecutiveReportController(IExecutiveReportService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Summary")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Report.Executive.Summary.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Summary([FromBody] jewelry.Model.Report.Executive.Summary.Request request)
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

        [Route("ProductionWip")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Report.Executive.ProductionWip.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionWip([FromBody] jewelry.Model.Report.Executive.ProductionWip.Request request)
        {
            try
            {
                var response = await _service.ProductionWip(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StalePlans")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StalePlans([FromBody] jewelry.Model.Report.Executive.StalePlans.Request request)
        {
            try
            {
                var response = await _service.StalePlans(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("Receivables")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Receivables([FromBody] jewelry.Model.Report.Executive.Receivables.Request request)
        {
            try
            {
                var response = await _service.Receivables(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("SalesOrdersWithoutInvoice")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SalesOrdersWithoutInvoice([FromBody] jewelry.Model.Report.Executive.SalesOrdersWithoutInvoice.Request request)
        {
            try
            {
                var response = await _service.SalesOrdersWithoutInvoice(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("StockHealth")]
        [HttpPost]
        [RequirePermission("executive:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Report.Executive.StockHealth.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> StockHealth([FromBody] jewelry.Model.Report.Executive.StockHealth.Request request)
        {
            try
            {
                var response = await _service.StockHealth(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
