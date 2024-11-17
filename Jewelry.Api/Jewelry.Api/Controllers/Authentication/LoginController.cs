using jewelry.Model.Exceptions;
using Jewelry.Api.Controllers.Production;
using Jewelry.Api.Extension;
using Jewelry.Service.Authentication.Login;
using Jewelry.Service.Production.Plan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Authentication
{
    [Route("Production/[controller]")]
    [ApiController]
    public class LoginController : ApiControllerBase
    {
        private readonly ILogger<PlanController> _logger;
        private readonly ILoginService _service;

        public LoginController(ILogger<PlanController> logger,
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
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ProductionPlanTransfer([FromBody] jewelry.Model.Authentication.Login.Request request)
        {
            var response = await _service.Login(request);
            return Ok(response);
        }
    }
}
