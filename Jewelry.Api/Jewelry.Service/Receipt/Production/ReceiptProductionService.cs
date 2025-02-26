using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
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
            var query = (from item in _jewelryContext.TbtStockProductReceiptPlan
                        .Include(o => o.TbtStockProductReceiptItem)

                         join plan in _jewelryContext.TbtProductionPlan
                         .Include(o => o.ProductTypeNavigation)
                         .Include(o => o.CustomerTypeNavigation)
                         .Include(o => o.TbtProductionPlanPrice)
                         on item.ProductionPlanId equals plan.Id

                         where item.Running == request.running
                         select new { item, plan }).FirstOrDefault();

            if (query == null)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> receipt plan");
            }

            var response = new jewelry.Model.Receipt.Production.PlanGet.Response()
            {
                Id = query.plan.Id,
                Wo = query.plan.Wo,
                WoNumber = query.plan.WoNumber,
                WoText = query.plan.WoText,
                ReceiptNumber = query.item.Running,
                ReceiptDate = query.item.CreateDate,
                IsCompleted = query.item.IsComplete,
                IsRunning = query.item.IsRunning,
                QtyRunning = query.item.QtyRunning,
                ProductNumber = query.plan.ProductNumber,
                ProductTypeName = query.plan.ProductTypeNavigation.NameTh,
                ProductType = query.plan.ProductType,
                ProductQty = query.plan.ProductQty,
                Mold = query.plan.Mold,
                Gold = query.plan.Type,
                GoldSize = query.plan.TypeSize,

                Stocks = new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>()
            };

            if (!string.IsNullOrEmpty(query.item.JsonDraft))
            {
                var draft = query.item.GetStocksFromJsonDraft();
                response.Stocks.AddRange(draft);
            }
            else
            {
                // คำนวณราคาต้นทุนและราคาต่อชิ้น
                decimal totalCost = 0;
                decimal pricePerUnit = 0;

                if (query.plan.TbtProductionPlanPrice != null && query.plan.TbtProductionPlanPrice.Any())
                {
                    totalCost = query.plan.TbtProductionPlanPrice.Sum(x => x.TotalPrice);

                    // ตรวจสอบว่าจำนวนสินค้ามากกว่า 0 ก่อนหารเพื่อป้องกัน DivideByZeroException
                    if (response.ProductQty > 0)
                    {
                        pricePerUnit = decimal.Round(totalCost / response.ProductQty, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        pricePerUnit = 0.00m;
                    }
                }

                //new draft
                var receiptStock = (from item in query.item.TbtStockProductReceiptItem
                                    select item).OrderBy(x => x.StockReceiptNumber);
                if (receiptStock.Any() == false)
                {
                    throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> receipt stocks");
                }
                int running = 1;
                foreach (var receipt in receiptStock)
                {
                    response.Stocks.Add(new jewelry.Model.Receipt.Production.PlanGet.ReceiptStock()
                    {
                        StockReceiptNumber = receipt.StockReceiptNumber,
                        StockNumber = string.Empty,

                        ProductNumber = $"{response.ProductNumber}-{running.ToString()}",
                        ProductNameTH = string.Empty,
                        ProductNameEN = string.Empty,

                        Qty = 1,
                        Price = pricePerUnit, // ใช้ราคาต่อชิ้นที่คำนวณไว้

                        Size = string.Empty,
                        Location = string.Empty,

                        ImageName = string.Empty,
                        ImageYear = null,
                        ImagePath = string.Empty,

                        Remark = string.Empty,
                        IsReceipt = false,

                        Materials = new List<jewelry.Model.Receipt.Production.PlanGet.Material>()
                    });
                    running++;
                }
            }

            return response;
        }

        public async Task<string> CreateDraft(jewelry.Model.Receipt.Production.Draft.Create.Request request)
        {

            var receiptPlan = (from item in _jewelryContext.TbtStockProductReceiptPlan
                               where item.Running == request.ReceiptNumber
                               select item).FirstOrDefault();

            if (receiptPlan == null)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> receipt no: {request.ReceiptNumber}");
            }

            receiptPlan.JsonDraft = request.MapToTbtStockProductReceiptPlanJson();
            receiptPlan.UpdateBy = CurrentUsername;
            receiptPlan.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtStockProductReceiptPlan.Update(receiptPlan);
            await _jewelryContext.SaveChangesAsync();

            return "success";
        }

    }
}
