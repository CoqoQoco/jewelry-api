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

    }
}

