using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Receipt.Lot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Receipt
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ReceiptLotController : ApiControllerBase
    {
        private readonly ILogger<ReceiptLotController> _logger;
        private readonly IReceiptLotService _receiptLot;

        public ReceiptLotController(
            ILogger<ReceiptLotController> logger,
            IReceiptLotService receiptLot,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _receiptLot = receiptLot;
        }

        [Route("Search")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Receipt.Lot.Search.Response>))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Search([FromBody] jewelry.Model.Receipt.Lot.Search.Request request)
        {
            try
            {
                var response = await _receiptLot.Search(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
