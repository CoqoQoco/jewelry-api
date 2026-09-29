using Microsoft.AspNetCore.Authorization;
using System;

namespace Jewelry.Api.Extension
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute : AuthorizeAttribute
    {
        public const string PolicyPrefix = "Permission:";

        public RequirePermissionAttribute(string permission)
            : base(PolicyPrefix + permission)
        {
        }

        // ANY of หลาย permission code — ผ่านเมื่อ user มี code ใดก็ได้ในรายการ เช่น
        // [RequirePermission("executive:view", "production:view")]
        public RequirePermissionAttribute(params string[] permissions)
            : base(PolicyPrefix + string.Join("|", permissions))
        {
        }
    }
}
