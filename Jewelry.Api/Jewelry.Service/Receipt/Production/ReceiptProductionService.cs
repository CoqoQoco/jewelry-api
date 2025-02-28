using jewelry.Model.Constant;
using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NetTopologySuite.Index.HPRtree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
                         .Include(o => o.TbtProductionPlanStatusHeader)
                         .ThenInclude(t => t.TbtProductionPlanStatusDetailGem)
                         on item.ProductionPlanId equals plan.Id

                         //join _gem in _jewelryContext.TbtProductionPlanStatusDetailGem
                         //on plan.Id equals _gem.ProductionPlanId into joinedGem
                         //from gems in joinedGem.DefaultIfEmpty()

                         where item.Running == request.running
                         select new { item, plan }).FirstOrDefault();

            if (query == null)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> receipt plan");
            }

            var response = new jewelry.Model.Receipt.Production.PlanGet.Response()
            {
                Id = query.plan.Id,
                ReceiptNumber = query.item.Running,

                Wo = query.plan.Wo,
                WoNumber = query.plan.WoNumber,
                WoText = query.plan.WoText,

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

            //get gems
            if (query.plan.TbtProductionPlanStatusHeader.Any())
            {
                var header = query.plan.TbtProductionPlanStatusHeader
                    .Where(x => x.Status == ProductionPlanStatus.Gems)
                    .FirstOrDefault();

                if (header != null
                    && header.TbtProductionPlanStatusDetailGem != null
                    && header.TbtProductionPlanStatusDetailGem.Any())
                {
                    response.Gems = header.TbtProductionPlanStatusDetailGem
                                    .Where(g => g.IsActive)
                                    .Select(g => new jewelry.Model.Receipt.Production.PlanGet.Gem
                                    {
                                        GemCode = g.GemCode,
                                        GemName = g.GemName,
                                        GemPrice = g.GemPrice,
                                        GemQty = g.GemQty,
                                        GemWeight = g.GemWeight,
                                    })
                                    .ToList();
                }
            }

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
                    if (response.ProductQty > 0 && totalCost > 0)
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

                query.item.JsonDraft = response.MapToTbtStockProductReceiptPlanJson();
                query.item.UpdateBy = CurrentUsername;
                query.item.UpdateDate = DateTime.UtcNow;

                _jewelryContext.TbtStockProductReceiptPlan.Update(query.item);
                await _jewelryContext.SaveChangesAsync();
            }

            return response;
        }

        public async Task<jewelry.Model.Receipt.Production.Confirm.Response> Confirm(jewelry.Model.Receipt.Production.Confirm.Request request)
        {
            var response = new jewelry.Model.Receipt.Production.Confirm.Response()
            {
                Stocks = new List<jewelry.Model.Receipt.Production.Confirm.Stock>()
            };

            var query = (from receipt in _jewelryContext.TbtStockProductReceiptPlan
                         .Include(o => o.TbtStockProductReceiptItem)

                         join plan in _jewelryContext.TbtProductionPlan
                         .Include(o => o.ProductTypeNavigation)
                         .Include(o => o.CustomerTypeNavigation)
                         .Include(o => o.TbtProductionPlanPrice)
                         .Include(o => o.TbtProductionPlanStatusHeader)
                         .ThenInclude(t => t.TbtProductionPlanStatusDetailGem)
                         on receipt.ProductionPlanId equals plan.Id

                         where receipt.Running == request.ReceiptNumber
                         select new { receipt, plan }).FirstOrDefault();

            if (query == null)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> receipt plan");
            }

            var productType = (from item in _jewelryContext.TbmProductType
                               where item.Code == query.plan.ProductType
                               select item).FirstOrDefault();

            if (productType == null)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> product type");
            }

            var drafts = query.receipt.GetStocksFromJsonDraft();

            if (drafts.Any() == false)
            {
                throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> draft");
            }

            var newStocks = new List<TbtStockProduct>();
            var newStocksMaterial = new List<TbtStockProductMaterial>();

            var updateReceipt = query.receipt;
            var updateReceiptItem = new List<TbtStockProductReceiptItem>();

            foreach (var stock in request.Stocks)
            {
                var match = query.receipt.TbtStockProductReceiptItem.FirstOrDefault(x => x.StockReceiptNumber == stock.StockReceiptNumber);

                if (match == null)
                {
                    throw new KeyNotFoundException($"{ErrorMessage.NotFound} --> {stock.StockReceiptNumber}");
                }
                if (match.IsReceipt)
                {
                    throw new HandleException($"{ErrorMessage.StockAlreadyReceipt} --> {stock.StockReceiptNumber}");
                }

                var _stockRunning = await _runningNumberService.GenerateRunningNumberForStockProduct(query.plan.ProductTypeNavigation.ProductCode);

                var newProduct = match.MapNewStockProduction(query.receipt, query.plan, stock, _stockRunning, CurrentUsername);
                var newProductResponse = newProduct.MapResponseNewStockProduction();
                newStocks.Add(newProduct);

                var newProductMaterial = new List<TbtStockProductMaterial>();
                if (stock.Materials != null && stock.Materials.Any())
                {
                    newProductMaterial = stock.MapNewStockProductionMaterial(_stockRunning, CurrentUsername);
                    newStocksMaterial.AddRange(newProductMaterial);

                    if (newProductMaterial.Any())
                    {
                        newProductResponse.Materials = newProductMaterial.MapResponseNewStockMaterialProduction();
                    }
                    response.Stocks.Add(newProductResponse);
                }

                match.IsReceipt = true;
                match.UpdateBy = CurrentUsername;
                match.UpdateDate = DateTime.UtcNow;
                updateReceiptItem.Add(match);

                foreach (var draft in drafts)
                {
                    if (draft.StockReceiptNumber == newProduct.ReceiptNumber)
                    {
                        draft.StockNumber = newProduct.StockNumber;

                        draft.ProductNumber = newProduct.ProductNumber;
                        draft.ProductNameTH = newProduct.ProductNameTh;
                        draft.ProductNameEN = newProduct.ProductNameEn;

                        draft.Qty = newProduct.Qty;
                        draft.Price = newProduct.ProductPrice;

                        draft.Size = newProduct.Size;
                        draft.Location = newProduct.Location;

                        draft.ImageName = newProduct.ImageName;
                        draft.ImagePath = newProduct.ImagePath;

                        draft.Remark = newProduct.Remark;
                        draft.IsReceipt = true;

                        draft.Materials = new List<jewelry.Model.Receipt.Production.PlanGet.Material>();

                        //map  draft.Materials by newStocksMaterial
                        if (newProductMaterial.Any())
                        {
                            foreach (var _newStocksMaterial in newProductMaterial)
                            {
                                draft.Materials.Add(new jewelry.Model.Receipt.Production.PlanGet.Material
                                {
                                    Type = _newStocksMaterial.Type,
                                    TypeName = _newStocksMaterial.TypeName,
                                    TypeCode = _newStocksMaterial.TypeCode,
                                    TypeBarcode = _newStocksMaterial.TypeBarcode,

                                    Qty = _newStocksMaterial.Qty,
                                    Weight = _newStocksMaterial.Weight,

                                    Size = _newStocksMaterial.Size,
                                    Price = _newStocksMaterial.Price
                                });
                            }
                        }
                    }
                }
            }

            updateReceipt.UpdateBy = CurrentUsername;
            updateReceipt.UpdateDate = DateTime.UtcNow;
            updateReceipt.IsRunning = true;
            updateReceipt.QtyRunning += request.Stocks.Count;
            if (updateReceipt.QtyRunning >= updateReceipt.Qty)
            {
                updateReceipt.IsComplete = true;
            }

            var receiptTemp = new jewelry.Model.Receipt.Production.PlanGet.Response()
            {
                Stocks = new List<jewelry.Model.Receipt.Production.PlanGet.ReceiptStock>()
            };
            receiptTemp.Stocks.AddRange(drafts);
            updateReceipt.JsonDraft = receiptTemp.MapToTbtStockProductReceiptPlanJson();

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {

                if (newStocks.Any())
                {
                    _jewelryContext.TbtStockProduct.AddRange(newStocks);
                }
                if (newStocksMaterial.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(newStocksMaterial);
                }
                if (updateReceiptItem.Any())
                {
                    _jewelryContext.TbtStockProductReceiptItem.UpdateRange(updateReceiptItem);
                }
                if (updateReceipt != null)
                {
                    _jewelryContext.TbtStockProductReceiptPlan.Update(updateReceipt);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return response;

        }

        public async Task<string> Darft(jewelry.Model.Receipt.Production.Draft.Create.Request request)
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
