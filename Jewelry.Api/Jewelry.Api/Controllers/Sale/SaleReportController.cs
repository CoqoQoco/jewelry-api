using Jewelry.Api.Extension;
using Jewelry.Service.Sale.SaleReport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    }
}
