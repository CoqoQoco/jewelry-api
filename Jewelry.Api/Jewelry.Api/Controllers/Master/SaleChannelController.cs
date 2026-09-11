using jewelry.Model.Exceptions;
using jewelry.Model.Master.SaleChannel;
using Jewelry.Api.Extension;
using Jewelry.Service.Master.SaleChannel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Master
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class SaleChannelController : ApiControllerBase
    {
        private readonly ILogger<SaleChannelController> _logger;
        private readonly ISaleChannelService _service;

        public SaleChannelController(ILogger<SaleChannelController> logger,
            ISaleChannelService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("List")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<SaleChannelResponse>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> List([FromBody] SaleChannelListRequest request)
        {
            var response = await _service.List(request);
            return Ok(response);
        }

        [Route("Active")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<SaleChannelResponse>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Active()
        {
            var response = await _service.Active();
            return Ok(response);
        }

        [Route("Current")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SaleChannelResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Current()
        {
            var response = await _service.Current();
            return Ok(response);
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(SaleChannelResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] GetSaleChannelRequest request)
        {
            try
            {
                var response = await _service.Get(request.Code);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Create")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateSaleChannelRequest request)
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

        [Route("Update")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update([FromBody] UpdateSaleChannelRequest request)
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

        [Route("Delete")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete([FromBody] DeleteSaleChannelRequest request)
        {
            try
            {
                await _service.Delete(request.Code);
                return Ok();
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
