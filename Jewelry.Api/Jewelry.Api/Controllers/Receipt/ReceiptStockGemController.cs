using jewelry.Model.Exceptions;
using jewelry.Model.Receipt.Gem.Create;
using jewelry.Model.Receipt.Gem.Scan;
using jewelry.Model.Stock.Mold.CheckOut;
using Jewelry.Api.Extension;
using Jewelry.Service.Receipt.Gem;
using Jewelry.Service.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Receipt
{
    [Route("/[controller]")]
    [ApiController]
    public class ReceiptStockGemController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IReceiptStockGemService _service;

        public ReceiptStockGemController(ILogger<MoldController> logger,
           IReceiptStockGemService service,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("CreateGem")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateGem([FromBody] CreateRequest request)
        {
            try
            {
                var response = await _service.CreateGem(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Scan")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(ScanResponse))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Scan([FromBody] ScanRequest request)
        {
            try
            {
                var result = await _service.Scan(request);
                if (!result.Any())
                {
                    return BadRequest(new NotFoundResponse() { Message = "ไม่พบข้อมูล" });
                }

                var response = result.FirstOrDefault();
                if (response == null)
                {
                    return BadRequest(new NotFoundResponse() { Message = "ไม่พบข้อมูล" });
                }
                //if (response.Quantity <= 0)
                //{
                //    return BadRequest(new NotFoundResponse() { Message = "ข้อมูลไม่พร้อมใช้งาน [จำนวนคงคลังไม่เพียงพอ]" });
                //}

                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
