using jewelry.Model.Exceptions;
using jewelry.Model.User;
using jewelry.Model.User.PasswordHash;
using jewelry.Model.User.UserActive;
using jewelry.Model.User.UserCreate;
using jewelry.Model.User.UserList;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.User
{
    public interface IUserService
    {
        Task<string> UserCreate(UserCreateRequest request);
        UserInfo UserGet(UserGet request);
        IQueryable<UserInfo> UserList(UserList request);
        Task<string> UserActive(UserActiveRequest request);
    }
    public class UserService : IUserService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IPasswordHash _passwordHashService;

        private readonly string _admin = "@ADMIN";
        public UserService(JewelryContext JewelryContext, IPasswordHash passwordHashService)
        {
            _jewelryContext = JewelryContext;
            _passwordHashService = passwordHashService;

        }

        #region *** public Method ***
        public async Task<string> UserCreate(UserCreateRequest request)
        {
            //check dubplicate username
            var checkUser = _jewelryContext.TbtUser.Where(x => x.Username.ToLower() == request.UserName.ToLower()).FirstOrDefault();
            if (checkUser != null)
            {
                throw new HandleException($"ชื่อผู้ใช้ '{request.UserName}' ถูกใช้งานแล้ว");
            }

            var passwordHash = _passwordHashService.CreateHash(request.Password);
            var newUser = new TbtUser
            {
                Username = request.UserName,
                PasswordHash = passwordHash.HashedPassword,
                PasswordSalt = passwordHash.Salt,

                FirstNameTh = request.FirstNameTH,
                LastNameTh = request.LastNameTH,
                PrefixNameTh = request.PrefixTH,

                Position = request.Position,
                PermissionLevel = request.Level,

                IsActive = true,

                CreateDate = DateTime.UtcNow,
                CreateBy = _admin,
            };

            _jewelryContext.TbtUser.Add(newUser);
            await _jewelryContext.SaveChangesAsync();

            return "Success";
        }
        public UserInfo UserGet(UserGet request)
        {
            var query = (from item in _jewelryContext.TbtUser
                         where item.Id == request.Id && item.Username == request.UserName
                         select new UserInfo()
                         {
                             Id = item.Id,
                             UserName = item.Username,

                             FirstNameTH = item.FirstNameTh,
                             LastNameTH = item.LastNameTh,
                             PrefixTH = item.PrefixNameTh,

                             IsActive = item.IsActive,

                             CreateDate = item.CreateDate,
                             CreateBy = item.CreateBy,
                         }).FirstOrDefault();

            if (query == null)
            {
                throw new HandleException($"ไม่พบข้อมูลผู้ใช้ '{request.UserName}'");
            }

            return query;
        }
        public IQueryable<UserInfo> UserList(UserList request)
        {
            var query = (from item in _jewelryContext.TbtUser
                         select new UserInfo()
                         {
                             Id = item.Id,
                             UserName = item.Username,

                             FirstNameTH = item.FirstNameTh,
                             LastNameTH = item.LastNameTh,
                             PrefixTH = item.PrefixNameTh,

                             IsActive = item.IsActive,

                             CreateDate = item.CreateDate,
                             CreateBy = item.CreateBy,
                         });

            if (!string.IsNullOrEmpty(request.Text))
            {
                query = query.Where(x => x.UserName.Contains(request.Text) || x.FirstNameTH.Contains(request.Text) || x.LastNameTH.Contains(request.Text));
            }

            return query;
        }
        public async Task<string> UserActive(UserActiveRequest request)
        {
            var user = _jewelryContext.TbtUser.Where(x => x.Id == request.Id && x.Username == request.UserName).FirstOrDefault();
            if (user == null)
            {
                throw new HandleException($"ไม่พบข้อมูลผู้ใช้ '{request.UserName}'");
            }

            if (request.Active == 1)
            {
                user.IsActive = true;
            }
            if (request.Active == 0)
            {
                user.IsActive = false;
            }
            user.UpdateDate = DateTime.UtcNow;
            user.UpdateBy = _admin;

            _jewelryContext.TbtUser.Attach(user);
            await _jewelryContext.SaveChangesAsync();

            return "Success";
        }   
        #endregion
    }
}
