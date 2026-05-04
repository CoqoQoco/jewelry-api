using jewelry.Model.Production.PrePlan;
using Jewelry.Api.Extension;
using Jewelry.Service.Production.PrePlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers.Production;

[Route("/[controller]")]
[ApiController]
[Authorize]
public class ProductionPrePlanController : ApiControllerBase
{
    private readonly IProductionPrePlanService _service;

    public ProductionPrePlanController(
        IProductionPrePlanService service,
        IOptions<ApiBehaviorOptions> apiBehaviorOptions)
        : base(apiBehaviorOptions)
    {
        _service = service;
    }

    [Route("Search")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Search([FromBody] SearchPrePlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ModelStateBadRequest();
            var response = await _service.Search(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("Get/{id}")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        try
        {
            var response = await _service.Get(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("Create")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreatePrePlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ModelStateBadRequest();
            var response = await _service.Create(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("Update/{id}")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdatePrePlanRequest request)
    {
        try
        {
            if (!ModelState.IsValid) return ModelStateBadRequest();
            var response = await _service.Update(id, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("Submit/{id}")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Submit([FromRoute] int id)
    {
        try
        {
            var response = await _service.Submit(id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
