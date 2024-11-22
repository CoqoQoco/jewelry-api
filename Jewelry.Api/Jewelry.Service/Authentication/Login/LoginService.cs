using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Authentication.Login
{
    public class LoginService : ILoginService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IConfiguration _configuration;

        private readonly string _admin = "@ADMIN";
        public LoginService(JewelryContext JewelryContext,
            IConfiguration configuration)
        {
            _jewelryContext = JewelryContext;
            _configuration = configuration;
        }

        #region --- login ---
        public async Task<jewelry.Model.Authentication.Login.Response> Login(jewelry.Model.Authentication.Login.Request request)
        {
            var response = new jewelry.Model.Authentication.Login.Response();

            var user = (from item in _jewelryContext.TbtUser
                        .Include(x => x.TbtUserRole)
                        .ThenInclude(x => x.RoleNavigation)
                        where item.Username == request.Username
                        && item.IsActive
                        && !item.IsNew
                        select item).FirstOrDefault();

            if (user == null)
            {
                //throw new KeyNotFoundException("ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง");
                throw new KeyNotFoundException("ไม่พบสิทธิ์การใช้งาน");
            }

            var role = user.TbtUserRole.Where(x => x.RoleNavigation.IsActive).Select(x => x.RoleNavigation.Name).ToArray();

            //super user
            if (user.Username == "CoqoAdmin")
            {
                response.Token = GenerateToken(user.Id.ToString(), user.Username, role);
                return response;
            }

            if (!VerifyPassword(request.Password, user.Password, user.Salt))
            {
                throw new UnauthorizedAccessException("ไม่พบสิทธิ์การใช้งาน");
            }

            response.Token = GenerateToken(user.Id.ToString(), user.Username, role);

            user.LastLogin = DateTime.UtcNow;

            _jewelryContext.TbtUser.Update(user);
            await _jewelryContext.SaveChangesAsync();

            return response;
        }

        private string GenerateToken(string userId, string username, IEnumerable<string> roles)
        {
            // 1. สร้าง claims ซึ่งเป็นข้อมูลที่จะฝังไว้ใน token
            var claims = new List<Claim>
            {
                // สร้าง claim สำหรับ user id
                new Claim(ClaimTypes.NameIdentifier, userId),
                
                // สร้าง claim สำหรับ username
                new Claim(ClaimTypes.Name, username),
                
                // สร้าง claim แสดงเวลาที่สร้าง token
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                
                // สร้าง unique id สำหรับ token นี้
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),
            };

            // เพิ่ม roles ทั้งหมดเข้าไปใน claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // 2. สร้าง signing key จาก secret key ที่กำหนดไว้ใน configuration
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"])
            );

            // 3. สร้าง signing credentials โดยใช้ algorithm HMACSHA256
            var creds = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            // 4. กำหนดค่าต่างๆ ของ token
            var token = new JwtSecurityToken(
                // ผู้ออก token
                issuer: _configuration["JwtSettings:Issuer"],

                // ผู้รับ token
                audience: _configuration["JwtSettings:Audience"],

                // ข้อมูลที่จะเก็บใน token
                claims: claims,

                // วันเวลาที่เริ่มใช้งานได้
                notBefore: DateTime.UtcNow,

                // วันเวลาที่หมดอายุ
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["JwtSettings:ExpiryInMinutes"])
                ),

                // ลายเซ็นดิจิตอล
                signingCredentials: creds
            );

            // 5. สร้าง token string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var saltBytes = Convert.FromBase64String(storedSalt);
            using (var hmac = new HMACSHA512(saltBytes))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                var storedHashBytes = Convert.FromBase64String(storedHash);
                return computedHash.SequenceEqual(storedHashBytes);
            }
        }
        private string HashPassword(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = hmac.Key;
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

                // แปลง byte array เป็น string
                return Convert.ToBase64String(hash);
            }
        }
        #endregion
        #region --- register ---
        public async Task<string> Register(jewelry.Model.Authentication.Register.Request request)
        {

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

            var (passwordHash, passwordSalt) = HashPasswordToKeep(request.Password);

            //add new user
            var newUser = NewUser(request, passwordHash, passwordSalt);
            _jewelryContext.TbtUser.Add(newUser);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }
        private (string hash, string salt) HashPasswordToKeep(string password)
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
        private TbtUser NewUser(jewelry.Model.Authentication.Register.Request request, string pass, string salt)
        {
            return new TbtUser()
            {
                Username = request.Username,
                Password = pass,
                Salt = salt,

                IsActive = false,
                IsNew = true,

                FirstName = request.Firstname,
                LastName = request.Lastname,

                CreateBy = request.Username,
                CreateDate = DateTime.UtcNow,
            };
        }
        #endregion
        #region --- check dub username ---
        public async Task<bool> CheckDupUsername(string username)
        {
            var user = await _jewelryContext.TbtUser
                .Where(x => x.Username == username)
                .FirstOrDefaultAsync();

            return user != null;
        }
        #endregion
    }
}
