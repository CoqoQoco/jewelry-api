using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem.Search;
using Jewelry.Api.Extension;
using Jewelry.Service.Stock;
using Jewelry.Service.Stock.Product;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jewelry.Api.Controllers.Stock
{
    [Route("/[controller]")]
    [ApiController]
    public class StockProductController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IProductService _service;

        public StockProductController(ILogger<MoldController> logger,
           IProductService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
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
    }
}
