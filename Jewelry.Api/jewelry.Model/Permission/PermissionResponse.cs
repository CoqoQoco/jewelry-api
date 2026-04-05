using System.Collections.Generic;

namespace jewelry.Model.Permission
{
    public class PermissionResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string? Description { get; set; }
    }

    public class RoleWithPermissionsResponse
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PermissionResponse> Permissions { get; set; } = new List<PermissionResponse>();
    }

    public class UpdateRolePermissionsRequest
    {
        public int RoleId { get; set; }
        public List<int> PermissionIds { get; set; } = new List<int>();
    }
}
