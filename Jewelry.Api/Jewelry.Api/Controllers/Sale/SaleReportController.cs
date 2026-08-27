using Jewelry.Api.Extension;
using Jewelry.Service.Sale.SaleReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleReportController : ApiControllerBase
    {
        private readonly ISaleReportService _service;

        public SaleReportController(ISaleReportService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("PipelineSummary")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.SaleReport.PipelineSummary.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> PipelineSummary([FromBody] jewelry.Model.Sale.SaleReport.PipelineSummary.Request request)
        {
            try
            {
                var response = await _service.PipelineSummary(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("CustomerProductionStatus")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Response>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CustomerProductionStatus([FromBody] jewelry.Model.Sale.SaleReport.CustomerProductionStatus.Request request)
        {
            try
            {
                var response = await _service.CustomerProductionStatus(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
