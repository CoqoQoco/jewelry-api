using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.ProductionPlan;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using NPOI.OpenXmlFormats.Dml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Sale.Quotation
{
    public class QuotationService : BaseService, IQuotationService
    {
        private readonly JewelryContext _jewelryContext;

        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public QuotationService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment,
            IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> Upsert(jewelry.Model.Sale.Quotation.Create.Request request)
        {

            if(string.IsNullOrEmpty(request.Number))
            {
                throw new HandleException("Quotation Number is Required.");
            }
           
            var quotation = (from item in _jewelryContext.TbtSaleQuotation
                             where item.Number == request.Number.ToUpper()
                             select item).FirstOrDefault();

            if (quotation == null)
            {
                quotation = new Data.Models.Jewelry.TbtSaleQuotation
                {
                    Number = request.Number.ToUpper(),
                    Running = await _runningNumberService.GenerateRunningNumberForGold("QUO"),

                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    CustomerAddress = request.CustomerAddress,

                    Currency = request.Currency,
                    CurrencyRate = request.CurrencyRate,

                    MarkUp = request.MarkUp,
                    Discount = request.Discount,

                    Remark = request.Remark,
                    Data = request.Data,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,

                    Freight = request.Freight,
                    SpecialDiscount = request.SpecialDiscount,
                    SpecialAddition = request.SpecialAddition,
                    Vat = request.Vat,
                    GoldPerOz = request.GoldPerOz,
                    Date = request.Date.HasValue ? request.Date.Value.UtcDateTime : DateTime.UtcNow
                };

                _jewelryContext.TbtSaleQuotation.Add(quotation);
                await _jewelryContext.SaveChangesAsync();

                return "success";
            }

            quotation.CustomerName = request.CustomerName;
            quotation.CustomerEmail = request.CustomerEmail;
            quotation.CustomerPhone = request.CustomerPhone;
            quotation.CustomerAddress = request.CustomerAddress;

            quotation.Currency = request.Currency;
            quotation.CurrencyRate = request.CurrencyRate;

            quotation.MarkUp = request.MarkUp;
            quotation.Discount = request.Discount;

            quotation.Remark = request.Remark;
            quotation.Freight = request.Freight;
            quotation.SpecialDiscount = request.SpecialDiscount;
            quotation.SpecialAddition = request.SpecialAddition;
            quotation.Vat = request.Vat;
            quotation.GoldPerOz = request.GoldPerOz;

            quotation.Data = request.Data;
            quotation.Date = request.Date.HasValue ? request.Date.Value.UtcDateTime : DateTime.UtcNow;

            quotation.UpdateDate = DateTime.UtcNow;
            quotation.UpdateBy = CurrentUsername;

            _jewelryContext.TbtSaleQuotation.Update(quotation);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }

        public async Task<jewelry.Model.Sale.Quotation.Get.Response> Get(jewelry.Model.Sale.Quotation.Get.Request request)
        { 
            if (string.IsNullOrEmpty(request.Number))
            {
                throw new HandleException("Quotation Number is Required.");
            }
            var quotation = (from item in _jewelryContext.TbtSaleQuotation
                             where item.Number == request.Number.ToUpper()
                             select item).FirstOrDefault();

            if (quotation == null)
            {
                throw new HandleException("Quotation Not Found.");
            }

            return new jewelry.Model.Sale.Quotation.Get.Response
            {
                Number = quotation.Number,
                Running = quotation.Running,

                CustomerName = quotation.CustomerName,
                CustomerPhone = quotation.CustomerPhone,
                CustomerEmail = quotation.CustomerEmail,
                CustomerAddress = quotation.CustomerAddress,

                Currency = quotation.Currency,
                CurrencyRate = quotation.CurrencyRate ?? 0 ,
                MarkUp = quotation.MarkUp ?? 0,
                Discount = quotation.Discount ?? 0,

                Remark = quotation.Remark,
                Data = quotation.Data,

                Freight = quotation.Freight.HasValue ? quotation.Freight.Value : null,
                SpecialDiscount = quotation.SpecialDiscount,
                SpecialAddition = quotation.SpecialAddition,
                Vat = quotation.Vat,
                GoldPerOz = quotation.GoldPerOz,
                Date = quotation.Date.HasValue ? quotation.Date.Value : null
            };
        }

        public IQueryable<jewelry.Model.Sale.Quotation.List.Response> List(jewelry.Model.Sale.Quotation.List.Request _request)
        {
            var request = _request.Search;
            var query = from quotation in _jewelryContext.TbtSaleQuotation
                        select new jewelry.Model.Sale.Quotation.List.Response
                        {
                            Number = quotation.Number ?? string.Empty,
                            Running = quotation.Running ?? string.Empty,
                            CustomerName = quotation.CustomerName ?? string.Empty,
                            CustomerPhone = quotation.CustomerPhone ?? string.Empty,
                            CustomerEmail = quotation.CustomerEmail ?? string.Empty,
                            CustomerAddress = quotation.CustomerAddress ?? string.Empty,
                            Currency = quotation.Currency ?? string.Empty,
                            CurrencyRate = quotation.CurrencyRate ?? 0,
                            MarkUp = quotation.MarkUp ?? 0,
                            Discount = quotation.Discount ?? 0,
                            Freight = quotation.Freight,
                            SpecialDiscount = quotation.SpecialDiscount,
                            SpecialAddition = quotation.SpecialAddition,
                            Vat = quotation.Vat,
                            GoldPerOz = quotation.GoldPerOz,
                            Remark = quotation.Remark ?? string.Empty,
                            Date = quotation.Date,
                            CreateDate = quotation.CreateDate,
                            CreateBy = quotation.CreateBy ?? string.Empty,
                            UpdateDate = quotation.UpdateDate,
                            UpdateBy = quotation.UpdateBy ?? string.Empty
                        };

            // Apply filters
            if (!string.IsNullOrEmpty(request.Number))
            {
                query = query.Where(x => x.Number.Contains(request.Number.ToUpper()));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                query = query.Where(x => x.CustomerName.Contains(request.CustomerName));
            }

            if (!string.IsNullOrEmpty(request.Currency))
            {
                query = query.Where(x => x.Currency.Contains(request.Currency));
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            if (request.CreateDateStart.HasValue)
            {
                query = query.Where(x => x.CreateDate >= request.CreateDateStart.Value.StartOfDayUtc());
            }

            if (request.CreateDateEnd.HasValue)
            {
                query = query.Where(x => x.CreateDate <= request.CreateDateEnd.Value.EndOfDayUtc());
            }

            if (request.QuotationDateStart.HasValue)
            {
                query = query.Where(x => x.Date.HasValue && x.Date.Value >= request.QuotationDateStart.Value.StartOfDayUtc());
            }

            if (request.QuotationDateEnd.HasValue)
            {
                query = query.Where(x => x.Date.HasValue && x.Date.Value <= request.QuotationDateEnd.Value.EndOfDayUtc());
            }

            return query;
        }
    }
}
