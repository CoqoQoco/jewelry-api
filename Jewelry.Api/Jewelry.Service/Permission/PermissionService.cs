using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.Permission
{
    public class PermissionService : BaseService, IPermissionService
    {
        private readonly JewelryContext _jewelryContext;

        public PermissionService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
        }

        public async Task<List<string>> GetCurrentUserPermissions()
        {
            var userRoles = CurrentUserRoles.ToList();

            if (!userRoles.Any())
                return new List<string>();

            var permissions = await (from rp in _jewelryContext.TbtRolePermission
                                    join role in _jewelryContext.TbmUserRole on rp.RoleId equals role.Id
                                    join perm in _jewelryContext.TbmPermission on rp.PermissionId equals perm.Id
                                    where userRoles.Contains(role.Name)
                                        && role.IsActive
                                        && perm.IsActive
                                    select perm.Code)
                                    .Distinct()
                                    .ToListAsync();

            return permissions;
        }

        public async Task<List<jewelry.Model.Permission.PermissionResponse>> GetAllPermissions()
        {
            return await _jewelryContext.TbmPermission
                .Where(p => p.IsActive)
                .OrderBy(p => p.GroupName)
                .ThenBy(p => p.Code)
                .Select(p => new jewelry.Model.Permission.PermissionResponse
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    GroupName = p.GroupName,
                    Description = p.Description
                })
                .ToListAsync();
        }

        public async Task<List<jewelry.Model.Permission.RoleWithPermissionsResponse>> GetAllRolesWithPermissions()
        {
            var roles = await _jewelryContext.TbmUserRole
                .Where(r => r.IsActive)
                .Include(r => r.TbtRolePermission)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.Level)
                .ToListAsync();

            return roles.Select(r => new jewelry.Model.Permission.RoleWithPermissionsResponse
            {
                RoleId = r.Id,
                RoleName = r.Name,
                Description = r.Description,
                Permissions = r.TbtRolePermission
                    .Where(rp => rp.Permission.IsActive)
                    .Select(rp => new jewelry.Model.Permission.PermissionResponse
                    {
                        Id = rp.Permission.Id,
                        Code = rp.Permission.Code,
                        Name = rp.Permission.Name,
                        GroupName = rp.Permission.GroupName,
                        Description = rp.Permission.Description
                    })
                    .OrderBy(p => p.GroupName)
                    .ThenBy(p => p.Code)
                    .ToList()
            }).ToList();
        }

        public async Task<string> UpdateRolePermissions(jewelry.Model.Permission.UpdateRolePermissionsRequest request)
        {
            CheckPermissionLevel("edit_user");

            // ลบ mapping เดิมทั้งหมดของ role นี้
            var existingMappings = await _jewelryContext.TbtRolePermission
                .Where(rp => rp.RoleId == request.RoleId)
                .ToListAsync();

            _jewelryContext.TbtRolePermission.RemoveRange(existingMappings);

            // เพิ่ม mapping ใหม่
            var newMappings = request.PermissionIds.Select(permId => new TbtRolePermission
            {
                RoleId = request.RoleId,
                PermissionId = permId,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            }).ToList();

            await _jewelryContext.TbtRolePermission.AddRangeAsync(newMappings);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
    }
}
