using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Helper;
using Jewelry.Service.User;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.User
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ApiControllerBase
    {
        private readonly ILogger<JewelryController> _logger;
        private readonly IUserService _service;
        public UserController(ILogger<JewelryController> logger,
            IUserService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }


        [Route("Get")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.User.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get()
        {
            var response = await _service.Get();
            return Ok(response);
        }

        [Route("GetAccount")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.User.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetAccount(int id)
        {
            var response = await _service.GetAccount(id);
            return Ok(response);
        }

        [Route("UpdateAccount")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateAccount([FromForm] jewelry.Model.User.UpdateAccount.Request request)
        {
            var response = await _service.UpdateAccount(request);
            return Ok(response);
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.User.List.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult List([FromBody] jewelry.Model.User.List.Request request)
        {
            var response = _service.List(request.Search);
            return response.ToDataSource(request);
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] jewelry.Model.User.Create.Request request)
        {
            var response = await _service.Create(request);
            return Ok(response);
        }

        [Route("Active")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Active([FromBody] jewelry.Model.User.Active.Request request)
        {
            var response = await _service.Active(request);
            return Ok(response);
        }
        [Route("Inactive")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Inactive([FromBody] jewelry.Model.User.Active.Request request)
        {
            var response = await _service.Inactive(request);
            return Ok(response);
        }

        [Route("ForceResetPassword")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> ForceResetPassword([FromBody] jewelry.Model.User.ForceResetPassword.Request request)
        {
            var response = await _service.ForceResetPassword(request);
            return Ok(response);
        }

        [Route("InactiveMyJob")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> InactiveMyJob([FromBody] jewelry.Model.User.InactiveMyJob.Request request)
        {
            var response = await _service.InactiveMyJob(request);
            return Ok(response);
        }

        [Route("ListMyJob")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.User.ListMyjob.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ListMyJob([FromBody] jewelry.Model.User.ListMyJob.Request request)
        {
            var response = _service.ListMyJob(request.Search);
            return response.ToDataSource(request);
        }

    }
}

