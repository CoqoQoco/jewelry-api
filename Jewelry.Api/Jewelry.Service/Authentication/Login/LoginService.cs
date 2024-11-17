using Jewelry.Data.Context;
using Jewelry.Service.Helper;
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
                        where item.Username == request.Username
                        && item.IsActive
                        select item).FirstOrDefault();

            if (user == null)
            {
                throw new KeyNotFoundException("Username/Password ไม่ถูกต้อง");
            }

            //test
            if (user.Username == "AdSystem")
            {
                response.Username = user.Username;
                response.Token = GenerateToken(user.Id.ToString(), user.Username, new List<string> { user.PermissionLevel.ToString() });

                response.PermissionLevel = user.PermissionLevel;
                response.FullName = $"{user.FirstNameTh} {user.LastNameTh}";

                return response;
            }

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
    }
}
