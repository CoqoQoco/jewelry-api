using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using Jewelry.Api.Extension;
using Jewelry.Data.Context;
using Jewelry.Service.Helper;
using Jewelry.Service.Sale.Quotation;
using Jewelry.Service.Stock.PlanReceipt;
using Jewelry.Service.Stock.Product;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Sale
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotationController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IQuotationService _service;

        public QuotationController(ILogger<MoldController> logger,
           IQuotationService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Upsert")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Sale.Quotation.Create.Request request)
        {
            try
            {
                var response = await _service.Upsert(request);
                return Ok(response);

            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Sale.Quotation.Get.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.Sale.Quotation.Get.Request request)
        {
            try
            {
                var response = await _service.Get(request);
                return Ok(response);

            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public IActionResult List([FromBody] jewelry.Model.Sale.Quotation.List.Request request)
        {
            try
            {
                var query = _service.List(request);
                var response = query.ToDataSourceResult(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
