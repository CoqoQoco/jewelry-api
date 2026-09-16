using jewelry.Model.GoldPrice;
using Jewelry.Api.Extension;
using Jewelry.Service.GoldPrice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class GoldPriceController : ApiControllerBase
    {
        private readonly ILogger<GoldPriceController> _logger;
        private readonly IGoldPriceService _service;

        public GoldPriceController(ILogger<GoldPriceController> logger,
            IGoldPriceService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Today")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(TodayGoldPriceResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Today([FromBody] TodayGoldPriceRequest request)
        {
            if (!ModelState.IsValid)
                return ModelStateBadRequest();

            var response = await _service.Today(request);
            return Ok(response);
        }

        [Route("History")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(HistoryGoldPriceResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> History([FromBody] HistoryGoldPriceRequest request)
        {
            if (!ModelState.IsValid)
                return ModelStateBadRequest();

            var response = await _service.History(request);
            return Ok(response);
        }
    }
}
