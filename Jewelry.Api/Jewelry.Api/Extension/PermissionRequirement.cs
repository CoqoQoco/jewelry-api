using Microsoft.AspNetCore.Authorization;
using System;

namespace Jewelry.Api.Extension
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        // รองรับหลาย permission code คั่นด้วย '|' (ANY of — ผ่านเมื่อ user มี code ใดก็ได้ในรายการ)
        // เดิมมีแค่ code เดียวเสมอ ก็ยังทำงานเหมือนเดิมทุกประการ (array ที่มี element เดียว)
        public string[] Permissions { get; }

        public PermissionRequirement(string permission)
        {
            Permissions = permission.Split('|', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
