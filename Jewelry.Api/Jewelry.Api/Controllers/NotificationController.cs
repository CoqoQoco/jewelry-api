using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Notification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ApiControllerBase
    {
        private readonly ILogger<NotificationController> _logger;
        private readonly INotificationService _service;

        public NotificationController(ILogger<NotificationController> logger,
            INotificationService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("MyCount")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Notification.MyCount.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MyCount()
        {
            try
            {
                var response = await _service.MyCount();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MyList")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Notification.MyList.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MyList([FromBody] jewelry.Model.Notification.MyList.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.MyList(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MarkRead")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MarkRead([FromBody] jewelry.Model.Notification.MarkRead.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.MarkRead(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MarkDone")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MarkDone([FromBody] jewelry.Model.Notification.MarkDone.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.MarkDone(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("Snooze")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> Snooze([FromBody] jewelry.Model.Notification.Snooze.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.Snooze(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("TeamList")]
        [HttpPost]
        [RequirePermission("notification:team")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(jewelry.Model.Notification.MyList.Response))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> TeamList([FromBody] jewelry.Model.Notification.TeamList.Request request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.TeamList(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
