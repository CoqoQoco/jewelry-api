using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Option;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock.PlanReceipt;
using Jewelry.Service.Stock.Product;
using Jewelry.Service.Stock.ProductImage;
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
    public class StockProductImageController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IProductImageService _service;

        public StockProductImageController(ILogger<MoldController> logger,
           IProductImageService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(OptionResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromForm] jewelry.Model.Stock.Product.Image.Create.Request request)
        {
            try
            {
                var response = await _service.Create(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Stock.Product.Image.List.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult List([FromBody] jewelry.Model.Stock.Product.Image.List.Request request)
        {
            try
            {
                var response = _service.List(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

    }
}
