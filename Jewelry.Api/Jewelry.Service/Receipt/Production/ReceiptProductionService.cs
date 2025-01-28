using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Receipt.Production
{
    public class ReceiptProductionService : BaseService, IReceiptProductionService
    {
        private readonly JewelryContext _jewelryContext;
        private IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;
        public ReceiptProductionService(JewelryContext JewelryContext, IHttpContextAccessor httpContextAccessor,
            IHostEnvironment HostingEnvironment, IRunningNumber runningNumberService) : base(JewelryContext, httpContextAccessor)
        {
            _jewelryContext = JewelryContext;
            _hostingEnvironment = HostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public IQueryable<jewelry.Model.Receipt.Production.PlanList.Response> ListPlan(jewelry.Model.Receipt.Production.PlanList.Search request)
        {
            var query = (from item in _jewelryContext.TbtStockProductReceiptPlan
                         .Include(o => o.TbtStockProductReceiptItem)

                         join plan in _jewelryContext.TbtProductionPlan
                         .Include(o => o.ProductTypeNavigation)
                         .Include(o => o.CustomerTypeNavigation)
                         on item.ProductionPlanId equals plan.Id

                         //.Include(x => x.ProductionPlan)
                         //.Include(o => o.ProductionPlan.ProductTypeNavigation)
                         //.Include(o => o.ProductionPlan.CustomerTypeNavigation)

                         select new jewelry.Model.Receipt.Production.PlanList.Response()
                         {
                             Id = plan.Id,
                             Wo = plan.Wo,
                             WoNumber = plan.WoNumber,
                             WoText = plan.WoText,

                             ReceiptNumber = item.Running,
                             ReceiptDate = item.CreateDate,

                             IsCompleted = item.IsComplete,
                             IsRunning = item.IsRunning,
                             QtyRunning = item.QtyRunning,

                             ProductNumber = plan.ProductNumber,
                             ProductTypeName = plan.ProductTypeNavigation.NameTh,
                             ProductType = plan.ProductType,
                             ProductQty = plan.ProductQty,

                             Mold = plan.Mold,
                             Gold = plan.Type,
                             GoldSize = plan.TypeSize,

                             ReceiptStocks = item.TbtStockProductReceiptItem.Select(x => new jewelry.Model.Receipt.Production.PlanList.ReceiptStock()
                             {
                                 StockNumber = x.StockReceiptNumber,
                                 IsReceipt = x.IsReceipt
                             })
                         });

            if (!string.IsNullOrEmpty(request.WO))
            {
                query = query.Where(x => x.WoText.Contains(request.WO));
            }

            return query;

        }
        public async Task<jewelry.Model.Receipt.Production.PlanGet.Response> GetPlan(jewelry.Model.Receipt.Production.PlanGet.Request request)
        {
            var response = (from item in _jewelryContext.TbtStockProductReceiptPlan
                          .Include(o => o.TbtStockProductReceiptItem)

                            join plan in _jewelryContext.TbtProductionPlan
                            .Include(o => o.ProductTypeNavigation)
                            .Include(o => o.CustomerTypeNavigation)
                            on item.ProductionPlanId equals plan.Id

                            //.Include(x => x.ProductionPlan)
                            //.Include(o => o.ProductionPlan.ProductTypeNavigation)
                            //.Include(o => o.ProductionPlan.CustomerTypeNavigation)

                            where item.Running == request.running

                            select new jewelry.Model.Receipt.Production.PlanGet.Response()
                            {
                                Id = plan.Id,
                                Wo = plan.Wo,
                                WoNumber = plan.WoNumber,
                                WoText = plan.WoText,

                                ReceiptNumber = item.Running,
                                ReceiptDate = item.CreateDate,

                                IsCompleted = item.IsComplete,
                                IsRunning = item.IsRunning,
                                QtyRunning = item.QtyRunning,

                                ProductNumber = plan.ProductNumber,
                                ProductTypeName = plan.ProductTypeNavigation.NameTh,
                                ProductType = plan.ProductType,
                                ProductQty = plan.ProductQty,

                                Mold = plan.Mold,
                                Gold = plan.Type,
                                GoldSize = plan.TypeSize,

                                ReceiptStocks = item.TbtStockProductReceiptItem.Select(x => new jewelry.Model.Receipt.Production.PlanGet.ReceiptStock()
                                {
                                    StockNumber = x.StockReceiptNumber,
                                    IsReceipt = x.IsReceipt
                                })
                            }).FirstOrDefault();

            if (response == null)
            {
                throw new KeyNotFoundException(ErrorMessage.NotFound);
            }

            return response;
        }

    }
}
