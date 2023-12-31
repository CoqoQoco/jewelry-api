﻿using jewelry.Model.Exceptions;
using jewelry.Model.Master;
using jewelry.Model.Mold;
using jewelry.Model.ProductionPlan.ProductionPlanCreate;
using Jewelry.Api.Extension;
using Jewelry.Service.ProductionPlan;
using Jewelry.Service.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    public class MoldController : ApiControllerBase
    {
        private readonly ILogger<MoldController> _logger;
        private readonly IMoldService _service;

        public MoldController(ILogger<MoldController> logger,
            IMoldService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }


        [Route("CreateMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateMold([FromForm] CreateMoldRequest request)
        {
            try
            {
                var response = await _service.CreateMold(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchMold")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted, Type = typeof(IQueryable<MasterModel>))]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public IActionResult SearchMold([FromBody] SearchMoldRequest request)
        {
            try
            {
                var response = _service.SearchMold(request.Search);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
