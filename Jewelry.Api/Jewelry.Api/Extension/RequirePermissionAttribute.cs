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
    }
}
