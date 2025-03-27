using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.Extensions.Hosting;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public interface IRunningNumber
    {
        Task<string> GenerateRunningNumber(string key);
        Task<string> GenerateRunningNumberForGold(string key);
        //Task<string> GenerateRunningNumberForStockProduct(string key);
        Task<string> GenerateRunningNumberForStockProductHash(string key);
    }
    public class RunningNumber : IRunningNumber
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public RunningNumber(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }
        private async Task<long> Next(string keys, int start = 0, int added = 1)
        {
            var number = await _jewelryContext.TbtRunningNumber.FindAsync(keys);

            if (number == null)
            {
                var newRunning = new TbtRunningNumber() 
                {
                    Key = keys,
                    Number = start + added,
                };

                _jewelryContext.TbtRunningNumber.Add(newRunning);
                await _jewelryContext.SaveChangesAsync();
                return start + added;
            }

            number.Number = number.Number + added;
            _jewelryContext.TbtRunningNumber.Update(number);
            await _jewelryContext.SaveChangesAsync();
            return number.Number;
        }

        public async Task<string> GenerateRunningNumber(string key)
        {
            var name = $"{key}{DateTime.UtcNow.ToString("yyyyMMdd")}";
            var jobRunning = await Next(name);
            name = $"{name}{jobRunning.ToString("0000")}";
            return name;
        }
        public async Task<string> GenerateRunningNumberForGold(string key)
        {
            var name = $"{key}{DateTime.UtcNow.ToString("yyMMdd")}";
            var jobRunning = await Next(name);
            name = $"{name}{jobRunning.ToString("000")}";
            return name;
        }

        public async Task<string> GenerateRunningNumberForStockProduct(string key)
        {
            var name = $"{key}-{DateTime.UtcNow.ToString("yyMM")}";
            var jobRunning = await Next(name);
            name = $"{name}-{jobRunning.ToString("000")}";
            return name;
        }

        public async Task<string> GenerateRunningNumberForStockProductHash(string key)
        {
            // แปลงปีเดือน (2403 → 6I3)
            int yearMonth = int.Parse(DateTime.UtcNow.ToString("yyMM"));
            string encodedMonth = ToBase36(yearMonth);

            var keyWithEncodedMonth = $"{key}-{encodedMonth}"; // ใช้ในฐานข้อมูลเพื่อรีเซ็ตเลข

            // Running number ที่รีเซ็ตทุกเดือน
            var monthlyRunning = await Next(keyWithEncodedMonth);

            // แสดงค่า Base36 ใน response
            return $"{key}-{encodedMonth}-{monthlyRunning:000}"; // return แบบที่ไม่สามารถเดาได้ง่าย
        }

        private string ToBase36(int number)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string result = "";
            while (number > 0)
            {
                result = chars[number % 36] + result;
                number /= 36;
            }
            return result.PadLeft(3, '0'); // ให้เป็น 3 ตัวอักษรเสมอ
        }
    }
}
