using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Gem;
using jewelry.Model.Stock.Mold.CheckOut;
using jewelry.Model.Stock.Mold.CheckOutList;
using jewelry.Model.Stock.Mold.Return;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Stock
{
    public interface IStockMoldService
    {
        IQueryable<CheckOutListResponse> SearchCheckOutList(CheckOutList request);
        Task<string> CheckOutMold(CheckOutRequest request);
        Task<string> ReturnMold(ReturnRequest request);
    }
    public class StockMoldService : IStockMoldService
    {
        private readonly string _admin = "@ADMIN";
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public StockMoldService(JewelryContext JewelryContext, IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<CheckOutListResponse> SearchCheckOutList(CheckOutList request)
        {

            var checkDate = DateTime.UtcNow.Date;

            var query = from item in _jewelryContext.TbtMoldCheckOutList
                        .Include(x => x.MoldCodeNavigation)
                        where item.IsActive == true && item.Status == 1
                        select new CheckOutListResponse()
                        {
                            Id = item.Id,
                            Running = item.Running,
                            Mold = item.MoldCode,

                            CreateDate = item.CreateDate,
                            CreateBy = item.CreateBy,
                            UpdateBy = item.UpdateBy,
                            UpdateDate = item.UpdateDate,

                            CheckOutDate = item.CheckOutDate,
                            CheckOutName = item.CheckOutName,
                            CheckOutDescription = item.CheckOutDescription,

                            ReturnDateSet = item.ReturnDateSet,
                            //ReturnDateSetLocal = item.ReturnDateSet.ToUniversalTime(),
                            ReturnName = item.ReturnName,
                            ReturnDescription = item.ReturnDescription,

                            IsOverReturn = item.ReturnDateSet.AddHours(7).Date < DateTime.UtcNow.Date,
                            IsSetReturn = item.ReturnDateSet.AddHours(7).Date == DateTime.UtcNow.Date,

                            Image = item.MoldCodeNavigation.Image,
                            Category = item.MoldCodeNavigation.Category,
                            CategoryCode = item.MoldCodeNavigation.CategoryCode
                        };

            var checkQuery = query.ToList();
            if (!string.IsNullOrEmpty(request.Text))
            {
                query = query.Where(x =>
                    x.Mold.Contains(request.Text)
                    || x.CheckOutName.Contains(request.Text)
                    || x.CheckOutDescription.Contains(request.Text)
                    || x.Running.Contains(request.Text));
            }

            if (request.CheckOutDateStart.HasValue)
            {
                query = query.Where(x => x.CheckOutDate >= request.CheckOutDateStart.Value.StartOfDayUtc());
            }
            if (request.CheckOutDateEnd.HasValue)
            {
                query = query.Where(x => x.CheckOutDate <= request.CheckOutDateEnd.Value.EndOfDayUtc());
            }
            if (request.ReturnDateStart.HasValue)
            {
                query = query.Where(x => x.ReturnDateSet >= request.ReturnDateStart.Value.StartOfDayUtc());
            }
            if (request.ReturnDateEnd.HasValue)
            {
                query = query.Where(x => x.ReturnDateSet <= request.ReturnDateEnd.Value.EndOfDayUtc());
            }

            return query;
        }
        public async Task<string> CheckOutMold(CheckOutRequest request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Mold 
                        select item).FirstOrDefault();

            if (mold == null)
            {
                throw new HandleException("ไม่พบเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }
            if (mold.Status != 1)
            {
                throw new HandleException("เเม่พิมพ์อยู่ระหว่างการเบิกใช้งาน กรุณาลองใหม่อีกครั้ง");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                
                var running = await _runningNumberService.GenerateRunningNumber("CHE");
                var checkOut = new TbtMoldCheckOutList()
                {
                    MoldCode = mold.Code,
                    Running = running,

                    CheckOutDate = request.CheckOutDate.UtcDateTime,
                    CheckOutName = request.CheckOutName,

                    CheckOutDescription = request.CheckOutDesc,
                    ReturnDateSet = request.ReturnOutDate.UtcDateTime,

                    IsActive = true,
                    Status = 1,
                   

                    CreateDate = DateTime.UtcNow,
                    CreateBy = _admin,
                    UpdateDate = DateTime.UtcNow,
                    UpdateBy = _admin
                };

                mold.Status = 2;
                mold.UpdateDate = DateTime.UtcNow;
                mold.UpdateBy = _admin;

                _jewelryContext.TbtMoldCheckOutList.Add(checkOut);
                _jewelryContext.TbtProductMold.Update(mold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }


            return "success";
        }
        public async Task<string> ReturnMold(ReturnRequest request)
        {
            var mold = (from item in _jewelryContext.TbtProductMold
                        where item.Code == request.Mold
                        select item).FirstOrDefault();

            if (mold == null)
            {
                throw new HandleException("ไม่พบเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }

            var checkOut = (from item in _jewelryContext.TbtMoldCheckOutList
                            where item.Id == request.Id
                            && item.MoldCode == request.Mold
                            && item.IsActive == true
                            select item).FirstOrDefault();

            if (checkOut == null)
            {
                throw new HandleException("ไม่พบรายการเบิกเเม่พิมพ์ กรุณาลองใหม่อีกครั้ง");
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                checkOut.ReturnDateFinish = request.ReturnDate.UtcDateTime;
                checkOut.ReturnDescription = request.ReturnDesc;
                checkOut.ReturnName = request.ReturnName;
                checkOut.Status = 2;

                checkOut.UpdateDate = DateTime.UtcNow;
                checkOut.UpdateBy = _admin;

                mold.Status = 1;
                mold.UpdateDate = DateTime.UtcNow;
                mold.UpdateBy = _admin;

                _jewelryContext.TbtMoldCheckOutList.Update(checkOut);
                _jewelryContext.TbtProductMold.Update(mold);
                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return "success";
        }
    }
}
