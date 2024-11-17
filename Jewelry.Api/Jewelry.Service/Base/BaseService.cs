using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Base
{
    public abstract class BaseService
    {
        protected readonly IHttpContextAccessor _httpContextAccessor;

        protected BaseService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Property สำหรับดึง username
        protected string CurrentUsername =>
            _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        // Property สำหรับดึง userId
        protected string CurrentUserId =>
           _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Property สำหรับดึง roles
        protected IEnumerable<string> CurrentUserRoles =>
            _httpContextAccessor.HttpContext?.User?
                .Claims.Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value) ?? Enumerable.Empty<string>();

        // Method สำหรับตรวจสอบ role
        protected bool IsInRole(string role) =>
            CurrentUserRoles.Contains(role);
    }
}
