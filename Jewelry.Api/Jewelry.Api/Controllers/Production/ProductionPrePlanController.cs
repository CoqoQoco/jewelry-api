using jewelry.Model.Production.PrePlan;
using Jewelry.Api.Extension;
using Jewelry.Service.Production.PrePlan;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
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

    [Route("Approve/{id}")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Approve([FromRoute] int id, [FromBody] ApprovePrePlanRequest request)
    {
        try
        {
            var response = await _service.Approve(id, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("Reject/{id}")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Reject([FromRoute] int id, [FromBody] RejectPrePlanRequest request)
    {
        try
        {
            var response = await _service.Reject(id, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("UploadApproveDocument")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadApproveDocumentResponse))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> UploadApproveDocument([FromForm] IFormFile file)
    {
        try
        {
            var path = await _service.UploadApproveDocument(file);
            return Ok(new UploadApproveDocumentResponse { Path = path });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Route("UploadProductImage")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadProductImageResponse))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> UploadProductImage([FromForm] UploadProductImageRequest request)
    {
        try
        {
            var imagePath = await _service.UploadProductImageAsync(request.File);
            return Ok(new UploadProductImageResponse { ImagePath = imagePath });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Route("CopyMoldDesignAsProductImage")]
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(UploadProductImageResponse))]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> CopyMoldDesignAsProductImage([FromBody] CopyMoldDesignRequest request)
    {
        try
        {
            var imagePath = await _service.CopyMoldDesignAsProductImageAsync(request.MoldDesignFilename);
            return Ok(new UploadProductImageResponse { ImagePath = imagePath });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Route("AvailableForPlan")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> AvailableForPlan([FromQuery] string? moldCode)
    {
        try
        {
            var response = await _service.GetAvailableForPlan(moldCode);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Route("WaitingCount")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> WaitingCount()
    {
        var count = await _service.GetWaitingCount();
        return Ok(new { count });
    }

    [Route("MasterJobType")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public IActionResult MasterJobType()
    {
        var list = new[]
        {
            new { Code = "NewDesign", Description = "งานแบบใหม่" },
            new { Code = "Sale", Description = "งานขาย" },
            //new { Code = "CustomCustomer", Description = "งานสั่งมีชื่อลูกค้า" },
            new { Code = "ReDesign", Description = "งานแปลง" },
            new { Code = "Order", Description = "งานสั่ง" },
        };
        return Ok(list);
    }

    [Route("MasterJobLocation")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public IActionResult MasterJobLocation()
    {
        var list = new[]
        {
            new { Code = "Domestic", Description = "งานในประเทศ" },
            new { Code = "Overseas", Description = "งานต่างประเทศ" },
        };
        return Ok(list);
    }

    [Route("MasterPrePlanStatus")]
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public IActionResult MasterPrePlanStatus()
    {
        var list = new[]
        {
            new { Code = "Draft", Description = "ร่าง" },
            new { Code = "Submitted", Description = "รออนุมัติ" },
            new { Code = "Approved", Description = "อนุมัติแล้ว" },
            new { Code = "PartiallyConsumed", Description = "สร้างแผนบางส่วน" },
            new { Code = "Consumed", Description = "สร้างแผนครบ" },
            new { Code = "Rejected", Description = "ปฏิเสธ" },
        };
        return Ok(list);
    }
}
