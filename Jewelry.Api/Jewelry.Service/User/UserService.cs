using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NPOI.OpenXmlFormats.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.User
{
    public class UserService : BaseService, IUserService
    {
        private readonly JewelryContext _jewelryContext;
        public UserService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
        }

        public jewelry.Model.User.Get.Response Get()
        {
            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        .ThenInclude(x => x.RoleNavigation)
                        where item.Username == CurrentUsername
                        && item.Id == int.Parse(CurrentUserId)
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            return new jewelry.Model.User.Get.Response()
            {
                PrefixNameTh = user.PrefixNameTh,
                FirstNameTh = user.FirstNameTh,
                LastNameTh = user.LastNameTh,
            };
        }

        #region --- create user ---
        public async Task<string> Create(jewelry.Model.User.Create.Request request)
        {
            //checkpermission
            CheckPermissionLevel("new_user");

            //check usernmae
            var (isValidUsername, errorUsername) = PasswordValidator.ValidateUsername(request.Username);
            if (!isValidUsername)
            {
                throw new HandleException(errorUsername);
            }

            //check password reg
            var (isValidPassword, errorPassword) = PasswordValidator.ValidatePassword(request.Password);
            if (!isValidPassword)
            {
                throw new HandleException(errorPassword);
            }

            var (passwordHash, passwordSalt) = HashPassword(request.Password);

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {   

                //add new user
                var newUser = NewUser(request, passwordHash, passwordSalt);
                _jewelryContext.TbtUser.Add(newUser);
                await _jewelryContext.SaveChangesAsync();

                //add role
                if (request.Roles != null && request.Roles.Any())
                {
                    var newUserRole = NewUserRole(request.Roles, request.Username, newUser.Id);
                    _jewelryContext.TbtUserRole.AddRange(newUserRole);
                    await _jewelryContext.SaveChangesAsync();
                }

                scope.Complete();
            }

            return "success";
        }
        private (string hash, string salt) HashPassword(string password)
        {
            // สร้าง salt แบบ random ด้วย RandomNumberGenerator (แทน RNGCryptoServiceProvider)
            byte[] saltBytes = new byte[64];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            // ใช้ HMACSHA512 เพื่อ hash password กับ salt
            using (var hmac = new HMACSHA512(saltBytes))
            {
                var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                // แปลงเป็น base64 string เพื่อเก็บใน database
                return (
                    hash: Convert.ToBase64String(hashBytes),
                    salt: Convert.ToBase64String(saltBytes)
                );
            }
        }
        private TbtUser NewUser(jewelry.Model.User.Create.Request request, string pass, string salt)
        {
            return new TbtUser()
            {
                Username = request.Username,
                Password = pass,
                Salt = salt,

                IsActive = true,
                IsNew = true,

                PrefixNameTh = request.PrefixNameTh,
                FirstNameTh = request.FirstNameTh,
                LastNameTh = request.LastNameTh,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow,
            };
        }
        private IEnumerable<TbtUserRole> NewUserRole(List<jewelry.Model.User.Create.Role> request, string username, int userId)
        {
            return from role in request
                   select new TbtUserRole()
                   {
                       UserId = userId,
                       Username = username,

                       Role = role.RoleId,

                       CreateBy = CurrentUsername,
                       CreateDate = DateTime.UtcNow,
                   };
        }
        #endregion
    }
}
