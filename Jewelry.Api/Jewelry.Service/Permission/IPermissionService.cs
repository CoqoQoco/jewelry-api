using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jewelry.Service.Permission
{
    public interface IPermissionService
    {
        Task<List<string>> GetCurrentUserPermissions();
        Task<List<jewelry.Model.Permission.PermissionResponse>> GetAllPermissions();
        Task<List<jewelry.Model.Permission.RoleWithPermissionsResponse>> GetAllRolesWithPermissions();
        Task<string> UpdateRolePermissions(jewelry.Model.Permission.UpdateRolePermissionsRequest request);
    }
}
