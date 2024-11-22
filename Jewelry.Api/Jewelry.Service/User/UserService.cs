using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
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

        #region --- get profile ---
        public jewelry.Model.User.Get.Response Get()
        {
            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        .ThenInclude(x => x.RoleNavigation)
                        where item.Username == CurrentUsername
                        && item.Id == int.Parse(CurrentUserId)
                        && item.IsActive
                        && item.IsNew == false
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            var response =  new jewelry.Model.User.Get.Response()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
            };

            if (user.TbtUserRole.Any())
            { 
                response.Role = from role in user.TbtUserRole
                                 select new jewelry.Model.User.Get.Role()
                                 {
                                     Id = role.RoleNavigation.Id,
                                     Level = role.RoleNavigation.Level,
                                     Name = role.RoleNavigation.Name,
                                     Description = role.RoleNavigation.Description ?? string.Empty,
                                 };
            }

            return response;
        }
        #endregion
        #region --- get account ---
        public jewelry.Model.User.GetAccount.Response GetAccount(int id)
        {
            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        .ThenInclude(x => x.RoleNavigation)
                        where item.Id == id
                        select item).FirstOrDefault();

            var masterUser = (from item in _jewelryContext.TbmUserRole
                              where item.IsActive
                              select item);

            if (user == null)
            {
                throw new KeyNotFoundException(ErrorMessage.NotFound);
            }

            var response = new jewelry.Model.User.GetAccount.Response()
            {
                Id = user.Id,
                Username = user.Username,

                FirstName = user.FirstName,
                LastName = user.LastName,

                IsActive = user.IsActive,
                IsNew = user.IsNew,

                CreatedDate = user.CreateDate,
                CreatedBy = user.CreateBy,
                UpdatedDate = user.UpdateDate,
                UpdatedBy = user.UpdateBy,
            };

            if (user.TbtUserRole.Any())
            {
                response.Roles = from role in user.TbtUserRole
                                 select new jewelry.Model.User.GetAccount.Role()
                                 {
                                     Id = role.RoleNavigation.Id,
                                     Name = role.RoleNavigation.Name,
                                     Description = role.RoleNavigation.Description ?? string.Empty,
                                 };
            }

            if (masterUser.Any())
            {
                response.MasterRoles = (from role in masterUser
                                        select new jewelry.Model.User.GetAccount.MasterRole()
                                        {
                                            Id = role.Id,
                                            Name = role.Name,
                                            Description = role.Description ?? string.Empty,
                                        });
            }

            return response;
        }
        #endregion
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

                FirstName = request.FirstName,
                LastName = request.LastName,

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
        #region --- list ---
        public IQueryable<jewelry.Model.User.List.Response> List(jewelry.Model.User.List.Search request)
        {
            var query = from user in _jewelryContext.TbtUser
                            //.Include(x => x.TbtUserRole)
                            //.ThenInclude(x => x.RoleNavigation)
                        select new jewelry.Model.User.List.Response()
                        {
                            Id = user.Id,
                            Username = user.Username,

                            FirstName = user.FirstName,
                            LastName = user.LastName,

                            IsActive = user.IsActive,
                            IsNew = user.IsNew,

                            CreatedDate = user.CreateDate,
                            CreatedBy = user.CreateBy,
                            UpdatedDate = user.UpdateDate,
                            UpdatedBy = user.UpdateBy,
                        };

            if (request.Id.HasValue)
            {
                query = query.Where(x => x.Id == request.Id.Value);
            }
            if (!string.IsNullOrEmpty(request.Username))
            {
                query = query.Where(x => x.Username.Contains(request.Username));
            }
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive);
            }
            if (request.IsNew.HasValue)
            {
                query = query.Where(x => x.IsNew == request.IsNew);
            }

            return query;
        }
        #endregion
        #region --- active ---
        public async Task<string> Active(jewelry.Model.User.Active.Request request)
        {
            CheckPermissionLevel("edit_user");

            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        where item.Id == request.Id
                        && item.Username == request.Username
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new KeyNotFoundException(ErrorMessage.NotFound);
            }
            if (request.IsNew != user.IsNew)
            {
                throw new KeyNotFoundException(ErrorMessage.InvalidRequest);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (!request.IsNew)
                {
                    if (user.TbtUserRole.Any())
                    {
                        _jewelryContext.TbtUserRole.RemoveRange(user.TbtUserRole);
                        await _jewelryContext.SaveChangesAsync();
                    }
                }

                if (request.Roles != null && request.Roles.Any())
                {
                    var newUserRole = NewUserRoleActive(request.Roles, user.Username, user.Id);

                    _jewelryContext.TbtUserRole.AddRange(newUserRole);
                    await _jewelryContext.SaveChangesAsync();
                }

                if (request.IsNew)
                {
                    user.IsNew = false;
                    user.IsActive = true;
                }

                user.UpdateDate = DateTime.UtcNow;
                user.UpdateBy = CurrentUsername;

                _jewelryContext.TbtUser.Update(user);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }


            return "success";
        }
        private IEnumerable<TbtUserRole> NewUserRoleActive(IEnumerable<jewelry.Model.User.Active.Role> request, string username, int userId)
        {
            return from role in request
                   select new TbtUserRole()
                   {
                       UserId = userId,
                       Username = username,

                       Role = role.Id,

                       CreateBy = CurrentUsername,
                       CreateDate = DateTime.UtcNow,
                   };
        }
        #endregion
    }
}
