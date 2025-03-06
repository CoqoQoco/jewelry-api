using Jewelry.Data.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
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
        private readonly JewelryContext _jewelryContext;

        protected BaseService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _jewelryContext = jewelryContext;
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

        protected void CheckPermissionLevel(string topic)
        {
            // ถ้าไม่มี user roles ให้ throw unauthorized ทันที
            if (!CurrentUserRoles.Any())
            {
                throw new UnauthorizedAccessException("ไม่พบข้อมูลสิทธิ์การใช้งาน");
            }

            // กำหนด permission map
            var permissionMap = new Dictionary<string, string[]>
            {
                ["new_user"] = new[] { "Admin", "Dev"},
                ["edit_user"] = new[] { "Admin", "Dev" },
                ["delete_user"] = new[] { "Admin", "Dev" },
            };

            // ตรวจสอบว่ามี topic นั้นใน map หรือไม่
            if (!permissionMap.ContainsKey(topic))
            {
                throw new KeyNotFoundException($"ไม่พบการกำหนดสิทธิ์สำหรับ {topic}");
            }

            // ตรวจสอบสิทธิ์
            var allowedRoles = permissionMap[topic];
            if (!allowedRoles.Any(role => CurrentUserRoles.Contains(role)))
            {
                throw new UnauthorizedAccessException($"คุณไม่มีสิทธิ์เข้าถึง: {topic}");
            }
        }
    }
}
