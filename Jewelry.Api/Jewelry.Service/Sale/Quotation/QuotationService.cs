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
                             where  request.Number == request.Number.ToUpper()
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
                    Discount = request.Dicount,

                    Remark = request.Remark,
                    Data = request.Data,

                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername,

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
            quotation.Discount = request.Dicount;

            quotation.Remark = request.Remark;

            quotation.Data = request.Data;
            quotation.Date = request.Date.HasValue ? request.Date.Value.UtcDateTime : DateTime.UtcNow;

            return "success";
        }

        public async Task<jewelry.Model.Sale.Quotation.Get.Response> Get(jewelry.Model.Sale.Quotation.Get.Request request)
        { 
            if (string.IsNullOrEmpty(request.Number))
            {
                throw new HandleException("Quotation Number is Required.");
            }
            var quotation = (from item in _jewelryContext.TbtSaleQuotation
                             where request.Number == request.Number.ToUpper()
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
                Dicount = quotation.Discount ?? 0,

                Remark = quotation.Remark,
                Data = quotation.Data,

                Date = quotation.Date.HasValue ? quotation.Date.Value : null
            };
        }
    }
}
