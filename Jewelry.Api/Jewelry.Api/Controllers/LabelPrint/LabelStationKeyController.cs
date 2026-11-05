using Jewelry.Api.Extension;
using Jewelry.Service.LabelPrint;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.LabelPrint
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize(Roles = "Dev")]
    public class LabelStationKeyController : ApiControllerBase
    {
        private readonly ILabelStationKeyService _service;

        public LabelStationKeyController(ILabelStationKeyService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _service = service;
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.LabelStationKey.Get.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Get([FromBody] jewelry.Model.LabelStationKey.Get.Request request)
        {
            var response = await _service.Get(request);
            return Ok(response);
        }

        [Route("Generate")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.LabelStationKey.Generate.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        [ProducesResponseType((int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> Generate([FromBody] jewelry.Model.LabelStationKey.Generate.Request request)
        {
            var response = await _service.Generate(request);
            return Ok(response);
        }
    }
}
