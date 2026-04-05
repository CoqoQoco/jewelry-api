using Jewelry.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Jewelry.Api.Extension
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PermissionAuthorizationHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var userRoles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            if (!userRoles.Any())
            {
                context.Fail();
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<JewelryContext>();

            var hasPermission = await (from rp in dbContext.TbtRolePermission
                                      join role in dbContext.TbmUserRole on rp.RoleId equals role.Id
                                      join perm in dbContext.TbmPermission on rp.PermissionId equals perm.Id
                                      where userRoles.Contains(role.Name)
                                          && role.IsActive
                                          && perm.IsActive
                                          && perm.Code == requirement.Permission
                                      select rp.Id)
                                      .AnyAsync();

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }
    }
}
