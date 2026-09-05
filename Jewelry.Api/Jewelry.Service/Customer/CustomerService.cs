using ICSharpCode.SharpZipLib.Zip;
using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using NPOI.XWPF.UserModel;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Jewelry.Service.Customer
{
    public interface ICustomerService
    {
        IQueryable<SearchCustomerResponse> Search(SearchCustomer request);
        IQueryable<SearchCustomerResponse> SearchCustomer(SearchCustomer request);
        Task<string> CreateCustomer(CreateCustomerRequest request);
        Task<string> UpdateCustomer(UpdateCustomerRequest request);
        Task<NextCustomerCodeResponse> GetNextCode(string prefix);
    }
    public class CustomerService : BaseService, ICustomerService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public CustomerService(JewelryContext JewelryContext, 
            IHostEnvironment HostingEnvironment,
            IHttpContextAccessor httpContextAccessor) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
        }

        class CustomerPlan
        {
            public TbmCustomer customer;
            public TbtProductionPlan Plan;
        }
        public IQueryable<SearchCustomerResponse> Search(SearchCustomer request)
        {
            // 1. สร้าง query พื้นฐานพร้อม Include ที่จำเป็น
            var query = _jewelryContext.TbmCustomer
                .Include(x => x.TypeCodeNavigation)
                .AsQueryable();

            // 2. กรองตามเงื่อนไขการค้นหาถ้ามี
            if (!string.IsNullOrEmpty(request.Text))
            {
                var searchText = request.Text.Trim();
                var searchTextUpper = searchText.ToUpper();

                var searchTextDigits = searchText
                    .Replace(" ", "").Replace("-", "").Replace("+", "")
                    .Replace("(", "").Replace(")", "").Replace(".", "");
                var isPhoneSearch = searchTextDigits.Length >= 6 && searchTextDigits.All(char.IsDigit);

                query = query.Where(item =>
                    item.Code.Contains(searchTextUpper) ||
                    (item.NameEn != null && item.NameEn.Contains(searchText)) ||
                    (item.NameTh != null && item.NameTh.Contains(searchText)) ||
                    (item.Email != null && item.Email.Contains(searchText)) ||
                    (item.ContactName != null && item.ContactName.Contains(searchText)) ||
                    (isPhoneSearch && item.Telephone1 != null && item.Telephone1 != "" &&
                        item.Telephone1.Replace(" ", "").Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(".", "").Contains(searchTextDigits)) ||
                    (isPhoneSearch && item.Telephone2 != null && item.Telephone2 != "" &&
                        item.Telephone2.Replace(" ", "").Replace("-", "").Replace("+", "").Replace("(", "").Replace(")", "").Replace(".", "").Contains(searchTextDigits))
                );
            }

            if (request.TypeCodes != null && request.TypeCodes.Any())
                query = query.Where(x => request.TypeCodes.Contains(x.TypeCode));
            if (request.DiscountMin.HasValue)
                query = query.Where(x => x.Discount >= request.DiscountMin.Value);
            if (request.DiscountMax.HasValue)
                query = query.Where(x => x.Discount <= request.DiscountMax.Value);

            // 3. ทำ LEFT JOIN กับตาราง TbtProductionPlan แบบปรับปรุง
            var result = query.GroupJoin(
                _jewelryContext.TbtProductionPlan,
                customer => customer.Code.ToUpper(),
                plan => plan.CustomerNumber.ToUpper(),
                (customer, plans) => new SearchCustomerResponse
                {
                    Code = customer.Code,
                    NameTh = customer.NameTh,
                    NameEn = customer.NameEn,
                    Address = customer.Address,
                    Telephone1 = customer.Telephone1,
                    Telephone2 = customer.Telephone2,
                    ContactName = customer.ContactName,
                    Email = customer.Email,
                    Remark = customer.Remark,
                    TypeCode = customer.TypeCode,
                    TypeName = customer.TypeCodeNavigation.NameTh,
                    Discount = customer.Discount,
                    TaxId = customer.TaxId,
                    ProductionPlanCount = plans.Count()
                });

            return result;
        }

        public IQueryable<SearchCustomerResponse> SearchCustomer(SearchCustomer request)
        {
            var response = (from item in _jewelryContext.TbmCustomer.Include(x => x.TypeCodeNavigation)
                            where item.Code.Contains(request.Text.ToUpper())
                            select new SearchCustomerResponse()
                            {
                                Code = item.Code,
                                NameTh = item.NameTh,
                                NameEn = item.NameEn,

                                Address = item.Address,
                                Telephone1 = item.Telephone1,
                                Telephone2 = item.Telephone2,
                                ContactName = item.ContactName,
                                Email = item.Email,
                                Remark = item.Remark,

                                TypeCode = item.TypeCode,
                                TypeName = item.TypeCodeNavigation.NameTh,
                                Discount = item.Discount,
                                TaxId = item.TaxId,
                            });

            return response;
        }

        public async Task<string> CreateCustomer(CreateCustomerRequest request)
        {
            if (request.AutoCode == true)
            {
                return await CreateCustomerWithAutoCode(request);
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new HandleException("กรุณาระบุรหัสลูกค้า");
            }

            var checkDub = (from item in _jewelryContext.TbmCustomer
                            where item.Code == request.Code
                            select item).SingleOrDefault();

            if (checkDub != null)
            {
                throw new HandleException($"พบรหัสลูกค้า {request.Code} ซ้ำในระบบ กรุณาสร้างรหัสใหม่");
            }

            var add = new TbmCustomer()
            {
                Code = request.Code.ToUpper(),
                NameTh = request.NameTH,
                NameEn = request.NameEN,

                Address = request.Address,
                TypeCode = request.Type,

                Telephone1 = request.Tel1,
                Telephone2 = request.Tel2,

                ContactName = request.ContactName,
                Email = request.Email,
                Remark = request.Remark,
                Discount = request.Discount ?? 0,
                TaxId = request.TaxId,

                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            _jewelryContext.TbmCustomer.Add(add);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code} - {request.NameTH}";
        }

        private const int AutoCodeMaxAttempts = 5;

        private async Task<string> CreateCustomerWithAutoCode(CreateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CodePrefix))
            {
                throw new HandleException("กรุณาระบุ CodePrefix สำหรับออกรหัสลูกค้าอัตโนมัติ");
            }

            for (var attempt = 1; attempt <= AutoCodeMaxAttempts; attempt++)
            {
                var code = await GenerateNextCode(request.CodePrefix);

                var add = new TbmCustomer()
                {
                    Code = code,
                    NameTh = request.NameTH,
                    NameEn = request.NameEN,

                    Address = request.Address,
                    TypeCode = request.Type,

                    Telephone1 = request.Tel1,
                    Telephone2 = request.Tel2,

                    ContactName = request.ContactName,
                    Email = request.Email,
                    Remark = request.Remark,
                    Discount = request.Discount ?? 0,
                    TaxId = request.TaxId,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                };

                _jewelryContext.TbmCustomer.Add(add);

                try
                {
                    await _jewelryContext.SaveChangesAsync();
                    return $"{code} - {request.NameTH}";
                }
                catch (DbUpdateException ex) when (IsCustomerCodeUniqueViolation(ex))
                {
                    _jewelryContext.Entry(add).State = EntityState.Detached;

                    if (attempt == AutoCodeMaxAttempts)
                    {
                        throw new HandleException($"ไม่สามารถออกรหัสลูกค้าอัตโนมัติ (prefix {request.CodePrefix}) ได้ กรุณาลองใหม่อีกครั้ง");
                    }
                }
            }

            throw new HandleException($"ไม่สามารถออกรหัสลูกค้าอัตโนมัติ (prefix {request.CodePrefix}) ได้ กรุณาลองใหม่อีกครั้ง");
        }

        private static bool IsCustomerCodeUniqueViolation(DbUpdateException ex)
        {
            return ex.InnerException is PostgresException pgEx
                && pgEx.SqlState == "23505"
                && (pgEx.ConstraintName == null || pgEx.ConstraintName == "tbm_customer_pk");
        }

        public async Task<NextCustomerCodeResponse> GetNextCode(string prefix)
        {
            var code = await GenerateNextCode(prefix);
            return new NextCustomerCodeResponse() { Code = code };
        }

        private async Task<string> GenerateNextCode(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new HandleException("กรุณาระบุ Prefix รหัสลูกค้า");
            }

            var prefixUpper = prefix.Trim().ToUpperInvariant();

            if (!Regex.IsMatch(prefixUpper, "^[A-Z]{1,5}$"))
            {
                throw new HandleException($"รูปแบบ Prefix ไม่ถูกต้อง ({prefix}) ต้องเป็นตัวอักษร A-Z ความยาว 1-5 ตัวอักษร");
            }

            // จำกัดขอบเขตด้วย StartsWith (แปลเป็น SQL LIKE ได้ใน EF Core + Npgsql)
            // แล้วดึงเฉพาะคอลัมน์ Code ของ prefix นี้มา parse ฝั่ง client เพราะ regex ไม่ translate เป็น SQL ได้
            var codes = await _jewelryContext.TbmCustomer
                .Where(x => x.Code.StartsWith(prefixUpper))
                .Select(x => x.Code)
                .ToListAsync();

            var pattern = new Regex($"^{Regex.Escape(prefixUpper)}(\\d+)$");
            long maxValue = 0;
            var digitLength = 3;

            foreach (var code in codes)
            {
                var match = pattern.Match(code?.ToUpperInvariant() ?? string.Empty);
                if (!match.Success)
                {
                    continue;
                }

                if (!long.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                if (value > maxValue)
                {
                    maxValue = value;
                    digitLength = match.Groups[1].Value.Length;
                }
            }

            var nextValue = maxValue + 1;
            var numberPart = nextValue.ToString().PadLeft(digitLength, '0');

            return $"{prefixUpper}{numberPart}";
        }

        public async Task<string> UpdateCustomer(UpdateCustomerRequest request)
        {
            var customer = (from item in _jewelryContext.TbmCustomer
                            where item.Code == request.Code.ToUpper()
                            select item).FirstOrDefault();

            if (customer == null)
            {
                throw new HandleException($"ไม่พบรหัสลูกค้า {request.Code} ในระบบ");
            }

            if (request.Type != null) customer.TypeCode = request.Type;
            if (request.NameTH != null) customer.NameTh = request.NameTH;
            if (request.NameEN != null) customer.NameEn = request.NameEN;
            if (request.Address != null) customer.Address = request.Address;
            if (request.Tel1 != null) customer.Telephone1 = request.Tel1;
            if (request.Tel2 != null) customer.Telephone2 = request.Tel2;
            if (request.Email != null) customer.Email = request.Email;
            if (request.ContactName != null) customer.ContactName = request.ContactName;
            if (request.Remark != null) customer.Remark = request.Remark;
            if (request.Discount.HasValue) customer.Discount = request.Discount.Value;
            if (request.TaxId != null) customer.TaxId = request.TaxId;

            customer.UpdateDate = DateTime.UtcNow;
            customer.UpdateBy = CurrentUsername;

            _jewelryContext.TbmCustomer.Update(customer);
            await _jewelryContext.SaveChangesAsync();

            return $"{customer.Code} - {customer.NameTh}";
        }
    }
}
