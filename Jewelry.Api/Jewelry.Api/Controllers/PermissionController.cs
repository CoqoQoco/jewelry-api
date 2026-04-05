using jewelry.Model.Exceptions;
using Jewelry.Api.Extension;
using Jewelry.Service.Permission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace Jewelry.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class PermissionController : ApiControllerBase
    {
        private readonly ILogger<PermissionController> _logger;
        private readonly IPermissionService _permissionService;

        public PermissionController(ILogger<PermissionController> logger,
            IPermissionService permissionService,
            IOptions<ApiBehaviorOptions> apiBehaviorOptions)
            : base(apiBehaviorOptions)
        {
            _logger = logger;
            _permissionService = permissionService;
        }

        [Route("MyPermissions")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<string>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetMyPermissions()
        {
            try
            {
                var response = await _permissionService.GetCurrentUserPermissions();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("All")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Permission.PermissionResponse>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var response = await _permissionService.GetAllPermissions();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("RolesWithPermissions")]
        [HttpGet]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(List<jewelry.Model.Permission.RoleWithPermissionsResponse>))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> GetRolesWithPermissions()
        {
            try
            {
                var response = await _permissionService.GetAllRolesWithPermissions();
                return Ok(response);
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }

        [Route("UpdateRolePermissions")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(string))]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> UpdateRolePermissions([FromBody] jewelry.Model.Permission.UpdateRolePermissionsRequest request)
        {
            try
            {
                var response = await _permissionService.UpdateRolePermissions(request);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new NotFoundResponse() { Message = ex.Message });
            }
            catch (HandleException ex)
            {
                return BadRequest(new NotFoundResponse() { Message = ex.Message });
            }
        }
    }
}
