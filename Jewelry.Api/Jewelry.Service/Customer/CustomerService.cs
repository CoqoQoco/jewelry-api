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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Customer
{
    public interface ICustomerService
    {
        IQueryable<SearchCustomerResponse> Search(SearchCustomer request);
        IQueryable<SearchCustomerResponse> SearchCustomer(SearchCustomer request);
        Task<string> CreateCustomer(CreateCustomerRequest request);
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

                query = query.Where(item =>
                    item.Code.Contains(searchTextUpper) ||
                    (item.NameEn != null && item.NameEn.Contains(searchText)) ||
                    (item.NameTh != null && item.NameTh.Contains(searchText)) ||
                    (item.Email != null && item.Email.Contains(searchText)) ||
                    (item.ContactName != null && item.ContactName.Contains(searchText))
                );
            }

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
                            });

            return response;
        }
        public async Task<string> CreateCustomer(CreateCustomerRequest request)
        {
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

                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            _jewelryContext.TbmCustomer.Add(add);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code} - {request.NameTH}";
        }
    }
}
