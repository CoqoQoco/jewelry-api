using jewelry.Model.Exceptions;
using jewelry.Model.Mold;
using jewelry.Model.Stock.Gem.Search;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock;
using Jewelry.Service.Stock.PlanReceipt;
using Jewelry.Service.Stock.Product;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class StockProductController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IProductService _service;
        private readonly IPlanReceiptService _planReceiptservice;

        public StockProductController(ILogger<MoldController> logger,
           IProductService service,
           IPlanReceiptService planReceiptservice,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
            _planReceiptservice = planReceiptservice;
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Stock.Product.List.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult List([FromBody] jewelry.Model.Stock.Product.List.Request request)
        {
            try
            {
                var response =  _service.List(request.Search);
                return response.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }


        [Route("Update")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update([FromBody] jewelry.Model.Stock.Product.Update.Request request)
        {
            try
            {
                var response = await _service.Update(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("PlanReceiptList")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Stock.Product.Plan.Receipt.List.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult PlanReceiptList([FromBody] jewelry.Model.Stock.Product.Plan.Receipt.List.Request request)
        {
            try
            {
                var response = _planReceiptservice.List(request.Search);
                return response.ToDataSource(request);

            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }
    }
}
