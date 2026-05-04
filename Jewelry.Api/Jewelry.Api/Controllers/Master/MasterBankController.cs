using jewelry.Model.Exceptions;
using jewelry.Model.Master.Bank;
using Jewelry.Api.Extension;
using Jewelry.Service.Master.Bank;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Master
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class MasterBankController : ApiControllerBase
    {
        private readonly ILogger<MasterBankController> _logger;
        private readonly IMasterBankService _service;

        public MasterBankController(ILogger<MasterBankController> logger,
            IMasterBankService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<BankResponse>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult List()
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = _service.GetBankList().ToList();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error listing banks");
                return StatusCode((int)HttpStatusCode.InternalServerError,
                    new { message = "An error occurred while listing banks" });
            }
        }
    }
}
