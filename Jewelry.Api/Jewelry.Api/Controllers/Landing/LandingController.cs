using jewelry.Model.Exceptions;
using Jewelry.Api.Controllers.Production;
using Jewelry.Api.Extension;
using Jewelry.Service.Authentication.Login;
using Jewelry.Service.Production.Plan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Landing
{
    [Route("/[controller]")]
    [ApiController]
    public class LandingController : ApiControllerBase
    {
        private readonly ILogger<PlanController> _logger;
        private readonly ILoginService _service;

        public LandingController(ILogger<PlanController> logger,
            ILoginService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Login")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Authentication.Login.Response))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ProductionPlanTransfer([FromBody] jewelry.Model.Authentication.Login.Request request)
        {
            var response = await _service.Login(request);
            return Ok(response);
        }

        [Route("Register")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Register([FromBody] jewelry.Model.Authentication.Register.Request request)
        {
            var response = await _service.Register(request);
            return Ok(response);
        }

        [Route("CheckDupUsername")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(bool))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CheckDupUsername(string username)
        {
            var response = await _service.CheckDupUsername(username);
            return Ok(response);
        }
    }
}
