using jewelry.Model.Customer;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
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
    public class CustomerService : ICustomerService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        public CustomerService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment)
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
            var CustomerPlan = (from item in _jewelryContext.TbmCustomer.Include(x => x.TypeCodeNavigation)
                                    //join plan in _jewelryContext.TbtProductionPlan on item.Code.ToUpper() equals plan.CustomerType.ToUpper() into joined
                                    //from j in joined.DefaultIfEmpty()
                                join plan in _jewelryContext.TbtProductionPlan on item.Code.ToUpper() equals plan.CustomerNumber.ToUpper() into planJoined
                                from pj in planJoined.DefaultIfEmpty()
                                where item.Code.Contains(request.Text.ToUpper())
                                select new CustomerPlan() { customer = item, Plan = pj });

            var customers = CustomerPlan.ToList();

            var response = (from item in CustomerPlan
                            group item by new
                            {
                                Code = item.customer.Code,
                            } into header
                            select new SearchCustomerResponse()
                            {
                                Code = header.Key.Code,
                                NameTh = header.First().customer.NameTh,
                                NameEn = header.First().customer.NameEn,

                                Address = header.First().customer.Address,
                                Telephone1 = header.First().customer.Telephone1,
                                Telephone2 = header.First().customer.Telephone2,
                                ContactName = header.First().customer.ContactName,
                                Email = header.First().customer.Email,
                                Remark = header.First().customer.Remark,

                                TypeCode = header.First().customer.TypeCode,
                                TypeName = header.First().customer.TypeCodeNavigation.NameTh,
                                OrderCount = header.Any(x => x.Plan != null) ? header.Select(x => x.Plan.Id).Count() : 0,
                            });

            //try
            //{
            //    var responses = response.ToList();
            //}
            //catch (Exception ex)
            //{ 
            //   throw new Exception(ex.Message, ex);
            //}
            return response.OrderBy(x => x.Code);
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

            return response.OrderBy(x => x.Code);
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
                CreateBy = _admin
            };

            _jewelryContext.TbmCustomer.Add(add);
            await _jewelryContext.SaveChangesAsync();

            return $"{request.Code} - {request.NameTH}";
        }
    }
}
