﻿using jewelry.Model.Exceptions;
using jewelry.Model.Mold.PlanGet;
using jewelry.Model.Receipt.Gem.Picklist;
using Jewelry.Api.Extension;
using Jewelry.Service.Receipt.Gem;
using Jewelry.Service.Receipt.Production;
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
    public class ReceiptProductionController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IReceiptProductionService _receiptProduction;

        public ReceiptProductionController(ILogger<MoldController> logger,
           IReceiptProductionService receiptProduction,
           IOptions<ApiBehaviorOptions> apiBehaviorOptions)
           : base(apiBehaviorOptions)
        {
            _logger = logger;
            _receiptProduction = receiptProduction;
        }

        [Route("ListPlan")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<jewelry.Model.Receipt.Production.PlanList.Response>))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public DataSourceResult ListPlan([FromBody] jewelry.Model.Receipt.Production.PlanList.Request request)
        {
            try
            {
                var response = _receiptProduction.ListPlan(request.Search);
                return response.ToDataSource(request);
            }
            catch (HandleException ex)
            {
                return new DataSourceResult() { Errors = BadRequest(new NotFoundResponse() { Message = ex.Message }), };
            }
        }

        [Route("GetPlan")]
        [HttpPost]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Accepted, Type = typeof(jewelry.Model.Receipt.Production.PlanGet.Response))]
        [ProducesResponseType((int)System.Net.HttpStatusCode.OK)]
        [ProducesResponseType((int)System.Net.HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetPlan(jewelry.Model.Receipt.Production.PlanGet.Request request)
        {
            try
            {
       

                var response = await _receiptProduction.GetPlan(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

    }
}
