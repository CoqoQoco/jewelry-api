using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Index.HPRtree;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.SS.Formula.Functions;
using NPOI.SS.Formula.PTG;
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
        private IHostEnvironment _hostingEnvironment;
        private IFileExtension _fileService;
        private readonly IAzureBlobStorageService _azureBlobService;

        public UserService(JewelryContext JewelryContext,
            IHostEnvironment hostingEnvironment,
            IFileExtension fileService,
            IAzureBlobStorageService azureBlobService,
            IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _fileService = fileService;
            _azureBlobService = azureBlobService;
        }

        #region --- get profile ---
        public async Task<jewelry.Model.User.Get.Response> Get()
        {
            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        .ThenInclude(x => x.RoleNavigation)
                        where item.Username == CurrentUsername
                        && item.Id == int.Parse(CurrentUserId)
                        && item.IsActive
                        && item.IsNew == false
                        select item).FirstOrDefault();

            var masterUser = (from item in _jewelryContext.TbmUserRole
                              where item.IsActive
                              select item);

            if (user == null)
            {
                throw new UnauthorizedAccessException();
            }

            var response = new jewelry.Model.User.Get.Response()
            {
                Id = user.Id,
                Username = user.Username,

                FirstName = user.FirstName,
                LastName = user.LastName,

                IsActive = user.IsActive,
                IsNew = user.IsNew,

                LastLogin = user.LastLogin,

                CreatedDate = user.CreateDate,
                CreatedBy = user.CreateBy,
                UpdatedDate = user.UpdateDate,
                UpdatedBy = user.UpdateBy,
            };

            if (user.TbtUserRole.Any())
            {
                response.Role = from role in user.TbtUserRole

                                select new jewelry.Model.User.Get.Role()
                                {
                                    Id = role.RoleNavigation.Id,
                                    Name = role.RoleNavigation.Name,
                                    Description = role.RoleNavigation.Description ?? string.Empty,
                                    Level = role.RoleNavigation.Level,
                                };
            }

            if (masterUser.Any())
            {
                response.MasterRoles = (from role in masterUser
                                        select new jewelry.Model.User.Get.MasterRole()
                                        {
                                            Id = role.Id,
                                            Name = role.Name,
                                            Description = role.Description ?? string.Empty,
                                        });
            }

            if (!string.IsNullOrEmpty(user.ImageUrl))
            {
                try
                {
                    // ตรวจสอบว่าเป็น blob path (User/filename.jpg) หรือ local path
                    if (user.ImageUrl.Contains("/") || user.ImageUrl.Contains("\\"))
                    {
                        // Blob path format: "User/filename.jpg"
                        var parts = user.ImageUrl.Split(new[] { '/', '\\' }, 2);
                        if (parts.Length == 2)
                        {
                            var folderName = parts[0];
                            var fileName = parts[1];

                            // ดึงจาก Azure Blob Storage
                            var stream = await _azureBlobService.DownloadImageAsync(folderName, fileName);
                            using (var memoryStream = new MemoryStream())
                            {
                                await stream.CopyToAsync(memoryStream);
                                byte[] imageBytes = memoryStream.ToArray();
                                response.Image = Convert.ToBase64String(imageBytes);
                            }
                        }
                    }
                    else
                    {
                        // Local path แบบเก่า (backwards compatibility)
                        response.Image = await _fileService.GetImageBase64String(user.ImageUrl, "Images/User/Profile");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - return profile without image
                    Console.WriteLine($"Failed to load user image: {ex.Message}");
                }
            }

            return response;
        }
        #endregion
        #region --- get account ---
        public async Task<jewelry.Model.User.GetAccount.Response> GetAccount(int id)
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

                LastLogin = user.LastLogin,

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

            if (!string.IsNullOrEmpty(user.ImageUrl))
            {
                try
                {
                    // ตรวจสอบว่าเป็น blob path (User/filename.jpg) หรือ local path
                    if (user.ImageUrl.Contains("/") || user.ImageUrl.Contains("\\"))
                    {
                        // Blob path format: "User/filename.jpg"
                        var parts = user.ImageUrl.Split(new[] { '/', '\\' }, 2);
                        if (parts.Length == 2)
                        {
                            var folderName = parts[0];
                            var fileName = parts[1];

                            // ดึงจาก Azure Blob Storage
                            var stream = await _azureBlobService.DownloadImageAsync(folderName, fileName);
                            using (var memoryStream = new MemoryStream())
                            {
                                await stream.CopyToAsync(memoryStream);
                                byte[] imageBytes = memoryStream.ToArray();
                                response.Image = Convert.ToBase64String(imageBytes);
                            }
                        }
                    }
                    else
                    {
                        // Local path แบบเก่า (backwards compatibility)
                        response.Image = await _fileService.GetImageBase64String(user.ImageUrl, "Images/User/Profile");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't throw - return profile without image
                    Console.WriteLine($"Failed to load user image: {ex.Message}");
                }
            }

            return response;
        }
        #endregion

        #region --- update ---
        public async Task<string> UpdateAccount(jewelry.Model.User.UpdateAccount.Request request)
        {

            var user = (from item in _jewelryContext.TbtUser
                        where item.Id == request.Id
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new KeyNotFoundException(ErrorMessage.NotFound);
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                if (!string.IsNullOrEmpty(request.ImageAction) && request.ImageAction == "update")
                {
                    var fileName = $"{user.Username}-{user.Id}.png";
                    try
                    {
                        // Upload to Azure Blob Storage (Single Container Architecture)
                        using var stream = request.Image.OpenReadStream();
                        var result = await _azureBlobService.UploadImageAsync(
                            stream,
                            "User",  // folder name in jewelry-images container
                            fileName
                        );

                        if (!result.Success)
                        {
                            throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {result.ErrorMessage}");
                        }

                        // เก็บ blob path: "User/filename.jpg"
                        user.ImageUrl = result.BlobName;
                    }
                    catch (Exception ex)
                    {
                        throw new HandleException($"ไม่สามารถบันทึกรูปภาพได้ {ex.Message}");
                    }
                }

                if (!string.IsNullOrEmpty(request.ImageAction) && request.ImageAction == "delete")
                {
                    // ลบรูปภาพจาก Azure Blob Storage ถ้ามี
                    if (!string.IsNullOrEmpty(user.ImageUrl))
                    {
                        try
                        {
                            // Parse folder and filename from path like "User/username-id.png"
                            var parts = user.ImageUrl.Split('/');
                            if (parts.Length == 2)
                            {
                                await _azureBlobService.DeleteImageAsync(parts[0], parts[1]);
                            }
                        }
                        catch (Exception)
                        {
                            // ไม่ต้อง throw error ถ้าลบไม่สำเร็จ
                        }
                    }

                    user.ImageUrl = string.Empty;
                }


                user.UpdateBy = CurrentUsername;
                user.UpdateDate = DateTime.UtcNow;
                _jewelryContext.TbtUser.Update(user);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
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

                ImageUrl = string.Empty,

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
            var query = (from user in _jewelryContext.TbtUser
                            .Include(x => x.TbtUserRole)
                            .ThenInclude(x => x.RoleNavigation)
                         select user);

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


            var response = (from user in query
                            select new jewelry.Model.User.List.Response()
                            {
                                Id = user.Id,
                                Username = user.Username,

                                FirstName = user.FirstName,
                                LastName = user.LastName,

                                IsActive = user.IsActive,
                                IsNew = user.IsNew,

                                LastLogin = user.LastLogin,

                                CreatedDate = user.CreateDate,
                                CreatedBy = user.CreateBy,
                                UpdatedDate = user.UpdateDate,
                                UpdatedBy = user.UpdateBy,

                                Image = user.ImageUrl,
                                RoleName = user.TbtUserRole.Any()
                                            ? user.TbtUserRole.OrderByDescending(x => x.RoleNavigation.Level).FirstOrDefault().RoleNavigation.Name
                                            : string.Empty,
                                RoleDescription = user.TbtUserRole.Any()
                                                    ? user.TbtUserRole.OrderByDescending(x => x.RoleNavigation.Level).FirstOrDefault().RoleNavigation.Description
                                                    : string.Empty,

                            });


            return response;
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
                }
                user.IsActive = true;

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
        #region --- inactive ---
        public async Task<string> Inactive(jewelry.Model.User.Active.Request request)
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
            //if (request.IsNew != user.IsNew)
            //{
            //    throw new KeyNotFoundException(ErrorMessage.InvalidRequest);
            //}
            //if (!user.IsActive)
            //{
            //    throw new KeyNotFoundException(ErrorMessage.InvalidRequest);
            //}

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                user.IsActive = false;
                user.IsNew = false;
                user.UpdateDate = DateTime.UtcNow;
                user.UpdateBy = CurrentUsername;

                _jewelryContext.TbtUser.Update(user);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }


            return "success";
        }
        #endregion


        #region --- force reset password (Admin only) ---
        public async Task<string> ForceResetPassword(jewelry.Model.User.ForceResetPassword.Request request)
        {
            CheckPermissionLevel("froce_password");

            // 2. หา User ที่ต้องการเปลี่ยน Password
            var targetUser = await _jewelryContext.TbtUser
                .Where(x => x.Username == request.TargetUsername && x.IsActive)
                .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                throw new KeyNotFoundException($"ไม่พบผู้ใช้งาน: {request.TargetUsername}");
            }

            // 3. Validate Password ใหม่
            var (isValidPassword, errorPassword) = PasswordValidator.ValidatePassword(request.NewPassword);
            if (!isValidPassword)
            {
                throw new HandleException(errorPassword);
            }

            // 4. Hash Password ใหม่
            var (passwordHash, passwordSalt) = HashPassword(request.NewPassword);

            // 5. Update Password
            targetUser.Password = passwordHash;
            targetUser.Salt = passwordSalt;
            targetUser.UpdateBy = CurrentUsername;
            targetUser.UpdateDate = DateTime.UtcNow;

            // ถ้าต้องการให้ user เปลี่ยน password ในครั้งแรกที่ login
            // targetUser.IsNew = true;

            _jewelryContext.TbtUser.Update(targetUser);
            await _jewelryContext.SaveChangesAsync();

            return $"เปลี่ยน Password ของ {request.TargetUsername} สำเร็จ";
        }


        #endregion


        #region list my job
        public IQueryable<jewelry.Model.User.ListMyjob.Response> ListMyJob(jewelry.Model.User.ListMyJob.Search request)
        {
            var query = (from job in _jewelryContext.TbtMyJob
                         select job);

            // Filter by Id
            if (request.Id != null && request.Id.Any())
            {
                query = query.Where(x => request.Id.Contains(x.Id));
            }

            // Filter by CreateBy (partial match with array)
            if (request.CreateBy != null && request.CreateBy.Any())
            {
                query = query.Where(x => request.CreateBy.Any(cb => x.CreateBy.Contains(cb)));
            }

            // Filter by IsActive
            if (request.IsActive.HasValue)
            {
                query = query.Where(x => x.IsActive == request.IsActive.Value);
            }

            // Filter by UpdateBy (partial match with array)
            if (request.UpdateBy != null && request.UpdateBy.Any())
            {
                query = query.Where(x => x.UpdateBy != null && request.UpdateBy.Any(ub => x.UpdateBy.Contains(ub)));
            }

            // Filter by StatusId
            if (request.StatusId != null && request.StatusId.Any())
            {
                query = query.Where(x => request.StatusId.Contains(x.StatusId));
            }

            // Filter by StatusName (partial match with array)
            if (request.StatusName != null && request.StatusName.Any())
            {
                query = query.Where(x => request.StatusName.Any(sn => x.StatusName.Contains(sn)));
            }

            // Filter by DataJob (partial match with array)
            if (request.DataJob != null && request.DataJob.Any())
            {
                query = query.Where(x => request.DataJob.Any(dj => x.DataJob.Contains(dj)));
            }

            // Filter by JobRunning (partial match with array)
            if (request.JobRunning != null && request.JobRunning.Any())
            {
                query = query.Where(x => request.JobRunning.Any(jr => x.JobRunning.Contains(jr)));
            }

            // Filter by JobTypeName (partial match with array)
            if (request.JobTypeName != null && request.JobTypeName.Any())
            {
                query = query.Where(x => request.JobTypeName.Any(jtn => x.JobTypeName.Contains(jtn)));
            }

            // Filter by JobTypeId
            if (request.JobTypeId != null && request.JobTypeId.Any())
            {
                query = query.Where(x => request.JobTypeId.Contains(x.JobTypeId));
            }

            // Filter by CreateDate range
            if (request.CreateDateFrom.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateFrom.Value);
            }
            if (request.CreateDateTo.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateDateTo.Value);
            }

            // Filter by UpdateDate range
            if (request.UpdateDateFrom.HasValue)
            {
                query = query.Where(x => x.UpdateDate.HasValue && x.UpdateDate.Value >= request.UpdateDateFrom.Value);
            }
            if (request.UpdateDateTo.HasValue)
            {
                query = query.Where(x => x.UpdateDate.HasValue && x.UpdateDate.Value <= request.UpdateDateTo.Value);
            }

            var response = from job in query
                           select new jewelry.Model.User.ListMyjob.Response()
                           {
                               Id = job.Id,
                               CreateBy = job.CreateBy,
                               CreateDate = job.CreateDate,
                               IsActive = job.IsActive,
                               UpdateBy = job.UpdateBy,
                               UpdateDate = job.UpdateDate,
                               StatusId = job.StatusId,
                               StatusName = job.StatusName,
                               DataJob = job.DataJob,
                               JobRunning = job.JobRunning,
                               JobTypeName = job.JobTypeName,
                               JobTypeId = job.JobTypeId
                           };

            return response;
        }

      
        #endregion

    }
}
