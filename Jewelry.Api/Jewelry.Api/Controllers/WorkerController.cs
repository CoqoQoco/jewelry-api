using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using jewelry.Model.Worker;
using Jewelry.Api.Extension;
using Jewelry.Service.Customer;
using Jewelry.Service.Worker;
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
    }
}
