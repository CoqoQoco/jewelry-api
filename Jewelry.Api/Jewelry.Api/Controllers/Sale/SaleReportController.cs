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

        [Route("ByChannel")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.SaleReport.ByChannel.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ByChannel([FromBody] jewelry.Model.Sale.SaleReport.ByChannel.Request request)
        {
            try
            {
                var response = await _service.ByChannel(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("SalesSummary")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.SaleReport.SalesSummary.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SalesSummary([FromBody] jewelry.Model.Sale.SaleReport.SalesSummary.Request request)
        {
            try
            {
                var response = await _service.SalesSummary(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("ProductGroupSales")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.SaleReport.ProductGroupSales.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductGroupSales([FromBody] jewelry.Model.Sale.SaleReport.ProductGroupSales.Request request)
        {
            try
            {
                var response = await _service.ProductGroupSales(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("TopDesignSales")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Sale.SaleReport.TopDesignSales.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> TopDesignSales([FromBody] jewelry.Model.Sale.SaleReport.TopDesignSales.Request request)
        {
            try
            {
                var response = await _service.TopDesignSales(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Route("InvoiceCustomerSuggest")]
        [HttpPost]
        [RequirePermission("sale:view")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Sale.SaleReport.InvoiceCustomerSuggest.Response>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> InvoiceCustomerSuggest([FromBody] jewelry.Model.Sale.SaleReport.InvoiceCustomerSuggest.Request request)
        {
            try
            {
                var response = await _service.InvoiceCustomerSuggest(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
