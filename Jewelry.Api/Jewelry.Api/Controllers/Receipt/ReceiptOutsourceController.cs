using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Receipt.Outsource;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Receipt
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class ReceiptOutsourceController : ApiControllerBase
    {
        private readonly ILogger<ReceiptOutsourceController> _logger;
        private readonly IReceiptOutsourceService _receiptOutsource;

        public ReceiptOutsourceController(
            ILogger<ReceiptOutsourceController> logger,
            IReceiptOutsourceService receiptOutsource,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _receiptOutsource = receiptOutsource;
        }

        [Route("Confirm")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Receipt.Outsource.Confirm.Response))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Confirm([FromBody] jewelry.Model.Receipt.Outsource.Confirm.Request request)
        {
            try
            {
                var response = await _receiptOutsource.Confirm(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("ListHistory")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Receipt.Outsource.History.List.Response>))]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public DataSourceResult ListHistory([FromBody] jewelry.Model.Receipt.Outsource.History.List.Request request)
        {
            try
            {
                var response = _receiptOutsource.ListHistory(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }) };
            }
        }
    }
}
