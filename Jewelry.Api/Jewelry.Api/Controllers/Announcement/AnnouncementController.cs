using jewelry.Model.Announcement;
using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Announcement;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers.Announcement
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class AnnouncementController : ApiControllerBase
    {
        private readonly ILogger<AnnouncementController> _logger;
        private readonly IAnnouncementService _service;

        public AnnouncementController(ILogger<AnnouncementController> logger,
            IAnnouncementService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("Feed")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(FeedAnnouncementResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Feed([FromBody] FeedAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Feed(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Get")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(AnnouncementItemResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Get([FromBody] GetAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Get(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Search")]
        [HttpPost]
        [RequirePermission("announcement:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Search([FromBody] SearchAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var result = await _service.Search(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Create")]
        [HttpPost]
        [RequirePermission("announcement:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(CreateAnnouncementResponse))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Create([FromForm] CreateAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

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
        [RequirePermission("announcement:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Update([FromForm] UpdateAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Update(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("TogglePublish")]
        [HttpPost]
        [RequirePermission("announcement:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> TogglePublish([FromBody] TogglePublishAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.TogglePublish(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Delete")]
        [HttpPost]
        [RequirePermission("announcement:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Delete([FromBody] DeleteAnnouncementRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Delete(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
