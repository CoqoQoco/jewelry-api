using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jewelry.Service.AiConnector.Auth
{
    public sealed class AiUserInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public List<string> Roles { get; set; } = new List<string>();
        public HashSet<string> Permissions { get; set; } = new HashSet<string>();
    }

    public interface IAiUserDirectory
    {
        // โหลด user + role + permission ล่าสุดจาก DB — null ถ้าไม่พบ / ปิดใช้งาน / ยังไม่ตั้งรหัสผ่านใหม่ (IsNew)
        Task<AiUserInfo?> Load(int userId);
    }

    public class AiUserDirectory : IAiUserDirectory
    {
        private readonly JewelryContext _context;

        public AiUserDirectory(JewelryContext context)
        {
            _context = context;
        }

        public async Task<AiUserInfo?> Load(int userId)
        {
            var user = await _context.TbtUser.AsNoTracking()
                .Where(u => u.Id == userId && u.IsActive && !u.IsNew)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName })
                .FirstOrDefaultAsync();
            if (user == null) return null;

            var roles = await (from ur in _context.TbtUserRole
                               join r in _context.TbmUserRole on ur.Role equals r.Id
                               where ur.UserId == userId && r.IsActive
                               select new { r.Id, r.Name }).Distinct().ToListAsync();
            var roleIds = roles.Select(r => r.Id).ToList();

            var perms = await (from rp in _context.TbtRolePermission
                               join p in _context.TbmPermission on rp.PermissionId equals p.Id
                               where roleIds.Contains(rp.RoleId) && p.IsActive
                               select p.Code).Distinct().ToListAsync();

            return new AiUserInfo
            {
                UserId = user.Id,
                Username = user.Username,
                DisplayName = (user.FirstName + " " + user.LastName).Trim(),
                Roles = roles.Select(r => r.Name).ToList(),
                Permissions = new HashSet<string>(perms),
            };
        }
    }
}
