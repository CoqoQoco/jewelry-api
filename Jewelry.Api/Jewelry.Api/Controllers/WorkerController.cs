using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using jewelry.Model.Worker;
using jewelry.Model.Worker.Create;
using jewelry.Model.Worker.List;
using Jewelry.Api.Extension;
using Jewelry.Service.Customer;
using Jewelry.Service.Worker;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class WorkerController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IWorkerService _service;

        public WorkerController(ILogger<MoldController> logger,
            IWorkerService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("GetWorkerProductionType")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(MasterWorkerProductionTypeResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult GetWorkerProductionType()
        {
            try
            {
                var response = _service.GetWorkerProductionType();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Search")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<ListWorkerProductionResponse>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult Search([FromBody] ListWorkerProductionRequest request)
        {
            try
            {
                var report = _service.Search(request.Search);
                return report.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateProductionWorkerRequest request)
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
    }
}
