using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public static class PasswordValidator
    {

        

        /// <summary>
        // -- > ความยาว:
        //ขั้นต่ำ 8 ตัวอักษร
        //ไม่เกิน 20 ตัวอักษร
        // -- > ต้องประกอบด้วย:
        //ตัวพิมพ์ใหญ่อย่างน้อย 1 ตัว
        //ตัวพิมพ์เล็กอย่างน้อย 1 ตัว
        //ตัวเลขอย่างน้อย 1 ตัว
        //อักขระพิเศษอย่างน้อย 1 ตัว
        // --> ข้อห้าม:
        //ห้ามมีตัวอักษรซ้ำติดกันเกิน 2 ตัว
        //ห้ามมีตัวเลขเรียงกัน(เช่น 123, 987)
        // --> การตรวจสอบอื่นๆ:
        //ต้องมีตัวอักษรที่ไม่ซ้ำกันอย่างน้อย 4 ตัว
        //อักขระพิเศษที่อนุญาตเฉพาะ !@#$%^&*(),.?":{}|<>
        /// </summary>
        private static class PasswordConfig
        {
            public const int MinLength = 8;
            public const int MaxLength = 20;
            public const int MinUniqueChars = 4;
            public const bool RequireUppercase = true;
            public const bool RequireLowercase = true;
            public const bool RequireDigit = true;
            public const bool RequireSpecialChar = true;
            public const bool RequireEng = true;
            public const bool RequireLetterStart = true;
        }

        public static (bool isValid, string errorMessage) ValidatePassword(string password)
        {
            var errors = new List<string>();

            // 1. ตรวจสอบความยาว
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password is required");
            }

            if (password.Length < PasswordConfig.MinLength)
            {
                errors.Add($"Password must be at least {PasswordConfig.MinLength} characters");
            }

            if (password.Length > PasswordConfig.MaxLength)
            {
                errors.Add($"Password must not exceed {PasswordConfig.MaxLength} characters");
            }

            // 2. ตรวจสอบตัวอักษรพิเศษที่ไม่อนุญาต
            var invalidChars = password.Where(c => !char.IsLetterOrDigit(c)
                && !IsAllowedSpecialChar(c)).ToList();
            if (invalidChars.Any())
            {
                errors.Add($"Password contains invalid special characters: {string.Join(", ", invalidChars)}");
            }

            // 3. ตรวจสอบตัวพิมพ์ใหญ่
            if (PasswordConfig.RequireUppercase && !password.Any(char.IsUpper))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }

            // 4. ตรวจสอบตัวพิมพ์เล็ก
            if (PasswordConfig.RequireLowercase && !password.Any(char.IsLower))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }

            // 5. ตรวจสอบตัวเลข
            if (PasswordConfig.RequireDigit && !password.Any(char.IsDigit))
            {
                errors.Add("Password must contain at least one number");
            }

            // 6. ตรวจสอบอักขระพิเศษ
            if (PasswordConfig.RequireSpecialChar && !password.Any(IsAllowedSpecialChar))
            {
                errors.Add("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>)");
            }

            // 7. ตรวจสอบจำนวนตัวอักษรที่ไม่ซ้ำกัน
            if (password.Distinct().Count() < PasswordConfig.MinUniqueChars)
            {
                errors.Add($"Password must contain at least {PasswordConfig.MinUniqueChars} unique characters");
            }

            // 8. ตรวจสอบตัวอักษรซ้ำติดกัน
            if (HasRepeatingCharacters(password, 3))
            {
                errors.Add("Password must not contain repeating characters more than 2 times in a row");
            }

            // 9. ตรวจสอบตัวเลขเรียงกัน
            if (HasSequentialNumbers(password))
            {
                errors.Add("Password must not contain sequential numbers (e.g., 123, 987)");
            }

            // รวมผลการตรวจสอบ
            return errors.Any()
                ? (false, string.Join(Environment.NewLine, errors))
                : (true, "Password is valid");
        }

        public static (bool isValid, string errorMessage) ValidateUsername(string username)
        {
            var errors = new List<string>();

            // 1. ตรวจสอบความยาว
            if (string.IsNullOrEmpty(username))
            {
                return (false, "Username is required");
            }

            if (username.Length < PasswordConfig.MinLength)
            {
                errors.Add($"Username must be at least {PasswordConfig.MinLength} characters");
            }

            if (username.Length > PasswordConfig.MaxLength)
            {
                errors.Add($"Username must not exceed {PasswordConfig.MaxLength} characters");
            }

            //required end
            if (PasswordConfig.RequireEng && !username.Any(char.IsLetter))
            {
                errors.Add("Username must contain at least one english letter");
            }

            //RequireLetterStart
            if (PasswordConfig.RequireLetterStart && !char.IsLetter(username[0]))
            {
                errors.Add("Username must start with a letter");
            }

            // รวมผลการตรวจสอบ
            return errors.Any()
                ? (false, string.Join(Environment.NewLine, errors))
                : (true, "Username is valid");
        }

        private static bool IsAllowedSpecialChar(char c)
        {
            return "!@#$%^&*(),.?\":{}|<>".Contains(c);
        }

        private static bool HasRepeatingCharacters(string password, int maxRepeat)
        {
            for (int i = 0; i <= password.Length - maxRepeat; i++)
            {
                if (password.Skip(i).Take(maxRepeat).All(c => c == password[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasSequentialNumbers(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (char.IsDigit(password[i]) &&
                    char.IsDigit(password[i + 1]) &&
                    char.IsDigit(password[i + 2]))
                {
                    int num1 = int.Parse(password[i].ToString());
                    int num2 = int.Parse(password[i + 1].ToString());
                    int num3 = int.Parse(password[i + 2].ToString());

                    if ((num2 == num1 + 1 && num3 == num2 + 1) || // เรียงขึ้น
                        (num2 == num1 - 1 && num3 == num2 - 1))   // เรียงลง
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
