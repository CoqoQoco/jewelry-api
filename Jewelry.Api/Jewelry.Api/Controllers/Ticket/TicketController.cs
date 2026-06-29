using jewelry.Model.Exceptions;
using jewelry.Model.Ticket;
using jewelry.Model.Ticket.Dashboard;
using Jewelry.Api.Extension;
using Jewelry.Service.Ticket;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using System.Threading.Tasks;

namespace Jewelry.Api.Controllers.Ticket
{
    [Route("/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketController : ApiControllerBase
    {
        private readonly ILogger<TicketController> _logger;
        private readonly ITicketService _service;

        public TicketController(ILogger<TicketController> logger,
            ITicketService service,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _service = service;
        }

        [Route("CreateTicket")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateTicket([FromForm] CreateTicketRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.CreateTicket(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("SearchTicket")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> SearchTicket([FromBody] SearchTicketRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var result = await _service.SearchTicket(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MyTicket")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(DataSourceResult))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MyTicket([FromBody] SearchTicketRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var result = await _service.GetMyTickets(request);
                return Ok(result);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UpdateStatus")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateTicketStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.UpdateTicketStatus(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UpdateDev")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateDev([FromBody] UpdateTicketDevRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.UpdateTicketDev(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("AddLog")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddLog([FromBody] AddTicketLogRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.AddTicketLog(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("AddComment")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddComment([FromBody] AddTicketCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.AddTicketComment(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("AddMyComment")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> AddMyComment([FromBody] AddMyTicketCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.AddMyTicketComment(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("DeleteComment")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteComment([FromBody] DeleteTicketCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.DeleteTicketComment(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("CountOpen")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(int))]
        public async Task<IActionResult> CountOpen()
        {
            return Ok(await _service.CountOpen());
        }

        [Route("Dashboard")]
        [HttpPost]
        [RequirePermission("ticket:manage")]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(TicketDashboardResponse))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetTicketDashboard([FromBody] TicketDashboardRequest request)
        {
            try
            {
                var response = await _service.GetTicketDashboard(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("MarkAsRead")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkTicketReadRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                await _service.MarkTicketAsRead(request.TicketId);
                return Ok("success");
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("DeleteMyComment")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> DeleteMyComment([FromBody] DeleteTicketCommentRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return ModelStateBadRequest();

                var response = await _service.DeleteMyTicketComment(request);
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
