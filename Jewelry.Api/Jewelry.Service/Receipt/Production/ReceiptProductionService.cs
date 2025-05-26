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
using NetTopologySuite.Triangulate.QuadEdge;
using NPOI.OpenXmlFormats.Dml;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                         on item.WoText equals plan.WoText

                         where item.IsComplete == false
                         //&& item.CreateDate >= request.ReceiptDateStart.StartOfDayUtc()
                         //&& item.CreateDate <= request.ReceiptDateEnd.EndOfDayUtc()
                         select new { item, plan });

            //var testtttt = query.ToList();

            if (request.ReceiptDateStart.HasValue)
            {
                query = query.Where(x => x.item.CreateDate >= request.ReceiptDateStart.Value.StartOfDayUtc());
            }

            //var testtt = query.ToList();

            if (request.ReceiptDateEnd.HasValue)
            {
                query = query.Where(x => x.item.CreateDate <= request.ReceiptDateEnd.Value.EndOfDayUtc());
            }

            //var testttt = query.ToList();

            if (!string.IsNullOrEmpty(request.ReceiptNumber))
            {
                query = query.Where(x => x.item.Running.Contains(request.ReceiptNumber.ToUpper()));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                query = query.Where(x => x.item.WoText.Contains(request.WoText.ToUpper()));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                query = query.Where(x => x.plan.Mold.Contains(request.Mold.ToUpper()));
            }

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.plan.ProductNumber.Contains(request.ProductNumber.ToUpper()));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.Select(x => x).ToArray();
                query = query.Where(x => productTypeArray.Contains(x.plan.ProductType));
            }

            if (request.GoldType != null && request.GoldType.Any())
            {
                var goldTypeArray = request.GoldType.Select(x => x).ToArray();
                query = query.Where(x => goldTypeArray.Contains(x.plan.Type));
            }
            if (request.GoldSize != null && request.GoldSize.Any())
            {
                var goldSizeArray = request.GoldSize.Select(x => x).ToArray();
                query = query.Where(x => goldSizeArray.Contains(x.plan.TypeSize));
            }

            var response = (from receip in query
                            select new jewelry.Model.Receipt.Production.PlanList.Response()
                            {
                                Id = receip.plan.Id,
                                Wo = receip.plan.Wo,
                                WoNumber = receip.plan.WoNumber,
                                WoText = receip.plan.WoText,

                                ReceiptNumber = receip.item.Running,
                                ReceiptDate = receip.item.CreateDate,

                                IsCompleted = receip.item.IsComplete,
                                IsRunning = receip.item.IsRunning,
                                QtyRunning = receip.item.QtyRunning,

                                ProductNumber = receip.plan.ProductNumber,
                                ProductTypeName = receip.plan.ProductTypeNavigation.NameTh,
                                ProductType = receip.plan.ProductType,
                                ProductQty = receip.plan.ProductQty,

                                Mold = receip.plan.Mold,
                                Gold = receip.plan.Type,
                                GoldSize = receip.plan.TypeSize,

                                ReceiptStocks = receip.item.TbtStockProductReceiptItem.Select(x => new jewelry.Model.Receipt.Production.PlanList.ReceiptStock()
                                {
                                    StockNumber = x.StockReceiptNumber,
                                    IsReceipt = x.IsReceipt
                                })
                            });


            return response;

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
                         on item.WoText equals plan.WoText

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
                ProductName = query.plan.ProductName,

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


            //have draft
            if (!string.IsNullOrEmpty(query.item.JsonDraft))
            {
                var draft = query.item.GetStocksFromJsonDraft();
                response.Stocks.AddRange(draft);
            }
            else
            {
                // คำนวณราคาต้นทุนและราคาต่อชิ้น
                decimal totalCost = 0;
                decimal pricePerUnit = 0.00m;

                //if (query.plan.TbtProductionPlanPrice != null && query.plan.TbtProductionPlanPrice.Any())
                //{
                //    totalCost = query.plan.TbtProductionPlanPrice.Sum(x => x.TotalPrice);

                //    // ตรวจสอบว่าจำนวนสินค้ามากกว่า 0 ก่อนหารเพื่อป้องกัน DivideByZeroException
                //    if (response.ProductQty > 0 && totalCost > 0)
                //    {
                //        pricePerUnit = decimal.Round(totalCost / response.ProductQty, 2, MidpointRounding.AwayFromZero);
                //    }
                //    else
                //    {
                //        pricePerUnit = 0.00m;
                //    }
                //}

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
                        StockNumber = null,

                        ProductNumber = $"{response.ProductNumber}-{running.ToString()}",
                        ProductNameTH = query.plan.ProductName,
                        ProductNameEN = query.plan.ProductName,

                        Qty = 1,
                        Price = pricePerUnit, // ใช้ราคาต่อชิ้นที่คำนวณไว้

                        Size = null,
                        StudEarring = null,
                        Location = null,

                        ImageName = null,
                        ImageYear = null,
                        ImagePath = null,

                        Remark = null,
                        IsReceipt = false,

                        MoldDesign = query.plan.Mold,

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
                         on receipt.WoText equals plan.WoText

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

            var cheackProductNumber = request.Stocks.Select(x => x.ProductNumber).ToArray();

            var duplicateStock = (from item in _jewelryContext.TbtStockProduct
                                  where cheackProductNumber.Contains(item.ProductNumber)
                                  select item);

            if (duplicateStock.Any())
            {
                // รวมรหัสสินค้าทั้งหมดเป็นข้อความเดียวโดยคั่นด้วยเครื่องหมาย comma
                var duplicateProductNumbers = string.Join(", ", duplicateStock.Select(x => x.ProductNumber));

                // โยน exception พร้อมข้อความที่รวมรายการรหัสสินค้าที่ซ้ำกัน
                throw new HandleException($"{ErrorMessage.AlreadyExist} --> รหัสสินค้า: {duplicateProductNumbers}");
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

                var prefix = query.plan.Type == "Silver" ? productType.SilverCode : productType.ProductCode;

                var _stockRunning = await _runningNumberService.GenerateRunningNumberForStockProductHash(prefix);

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
                }

                response.Stocks.Add(newProductResponse);

                match.IsReceipt = true;
                match.UpdateBy = CurrentUsername;
                match.UpdateDate = DateTime.UtcNow;
                match.ReceiptDate = DateTime.UtcNow;
                match.StockNumber = _stockRunning;

                match.Mold = query.plan.Mold;
                match.MoldDesign = stock.MoldDesign;

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
                        draft.StudEarring = newProduct.StudEarring;
                        draft.Location = newProduct.Location;

                        draft.ImageName = newProduct.ImageName;
                        draft.ImagePath = newProduct.ImagePath;

                        draft.Remark = newProduct.Remark;
                        draft.IsReceipt = true;

                        draft.MoldDesign = newProduct.MoldDesign;
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
                                    Region = _newStocksMaterial.Region,
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
        public IQueryable<jewelry.Model.Receipt.Production.History.List.Response> ListHistory(jewelry.Model.Receipt.Production.History.List.Search request)
        {
            var receipt = (from item in _jewelryContext.TbtStockProductReceiptItem
                           where item.IsReceipt == true
                           //&& item.ReceiptDate >= request.ReceiptDateStart.StartOfDayUtc()
                           //&& item.ReceiptDate <= request.ReceiptDateEnd.EndOfDayUtc()
                           select item);

            if (request.ReceiptDateStart.HasValue)
            {
                receipt = receipt.Where(x => x.ReceiptDate >= request.ReceiptDateStart.Value.StartOfDayUtc());
            }
            if (request.ReceiptDateEnd.HasValue)
            {
                receipt = receipt.Where(x => x.ReceiptDate <= request.ReceiptDateEnd.Value.EndOfDayUtc());
            }

            if (request.ReceiptType != null && request.ReceiptType.Any())
            {
                var receiptTypeArray = request.ReceiptType.Select(x => x).ToArray();
                receipt = receipt.Where(x => receiptTypeArray.Contains(x.Type));
            }
            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                receipt = receipt.Where(x => x.StockNumber.Contains(request.StockNumber));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                receipt = receipt.Where(x => x.Mold.Contains(request.Mold));
            }
            if (!string.IsNullOrEmpty(request.WoText))
            {
                receipt = receipt.Where(x => x.WoText.Contains(request.WoText));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.Select(x => x).ToArray();
                receipt = receipt.Where(x => productTypeArray.Contains(x.ProductType));
            }

            if (receipt.Any() == false)
            {
                return Enumerable.Empty<jewelry.Model.Receipt.Production.History.List.Response>().AsQueryable();
            }

            var query = (from item in receipt

                         join stock in _jewelryContext.TbtStockProduct
                         on item.StockNumber equals stock.StockNumber

                         select new jewelry.Model.Receipt.Production.History.List.Response()
                         {

                             StockNumber = stock.StockNumber,
                             Status = stock.Status,

                             ReceiptNumber = item.StockReceiptNumber,
                             ReceiptDate = stock.ReceiptDate,
                             ReceiptType = item.Type,

                             Mold = item.MoldDesign ?? item.Mold,
                             MoldDesign = item.MoldDesign,

                             Qty = stock.Qty,
                             ProductPrice = stock.ProductPrice,

                             ProductNumber = stock.ProductNumber,
                             ProductNameTh = stock.ProductNameTh,
                             ProductNameEn = stock.ProductNameEn,

                             ProductType = stock.ProductType,
                             ProductTypeName = item.ProductTypeName,

                             ImageName = stock.ImageName,
                             ImagePath = stock.ImagePath,

                             Wo = item.Wo,
                             WoNumber = item.WoNumber,
                             WoText = $"{item.Wo}{item.WoNumber.ToString()}",

                             ProductionDate = item.CreateDate,
                             ProductionType = item.ProductionType,
                             ProductionTypeSize = item.ProductionTypeSize,

                             Size = stock.Size,
                             Location = stock.Location,
                             Remark = stock.Remark,

                             StudEarring = stock.StudEarring,

                             CreateBy = item.CreateBy,
                             CreateDate = item.CreateDate,
                         });

            if (!string.IsNullOrEmpty(request.ProductNumber))
            {
                query = query.Where(x => x.ProductNumber.Contains(request.ProductNumber));
            }
            if (!string.IsNullOrEmpty(request.ProductNameTh))
            {
                query = query.Where(x => x.ProductNameTh.Contains(request.ProductNameTh));
            }
            if (!string.IsNullOrEmpty(request.ProductNameEn))
            {
                query = query.Where(x => x.ProductNameEn.Contains(request.ProductNameEn));
            }
            if (!string.IsNullOrEmpty(request.Size))
            {
                query = query.Where(x => x.Size.Contains(request.Size));
            }

            return query;

        }


        public async Task<string> ImportProduct()
        {
            var oldProduct = (from item in _jewelryContext.StockFromConvert
                              where item.Typejob != "convert"
                              select item).Take(10000).ToList();

            var actualProduct = (from item in _jewelryContext.TbtStockProduct
                                 select item).ToList();

            var masterProductType = (from item in _jewelryContext.TbmProductType
                                     select item).ToList();

            var masterGold = (from item in _jewelryContext.TbmGold
                              select item).ToList();

            var masterGoldSize = (from item in _jewelryContext.TbmGoldSize
                                  select item).ToList();


            if (!oldProduct.Any())
            {
                return "No Data";
            }

            var _receiptRunning = await _runningNumberService.GenerateRunningNumber("REP");

            var addProducts = new List<TbtStockProduct>();
            var addProductsMaterial = new List<TbtStockProductMaterial>();
            int add = 0;

            foreach (var item in oldProduct)
            {
                var match = actualProduct.Where(x => x.ProductCode == item.Noproduct).FirstOrDefault();
                if (match != null)
                {
                    continue;
                }

                string prefix = "DK";
                var _stockRunning = await _runningNumberService.GenerateRunningNumberForStockProductHash(prefix);

                add++;

                var newProduct = new TbtStockProduct
                {
                    StockNumber = _stockRunning,
                    Status = StockProductStatus.Available,

                    ReceiptNumber = _receiptRunning,
                    ReceiptDate = DateTime.UtcNow,
                    ReceiptType = "convert",

                    Mold = item.NoCode,
                    MoldDesign = item.NoCode,

                    Qty = item.Quantity.HasValue ? item.Quantity.Value : 1,

                    ProductPrice = item.Pricesale.HasValue ? item.Pricesale.Value : 0,
                    ProductCost = string.IsNullOrEmpty(item.Pricecost) ? 0 : decimal.Parse(item.Pricecost),

                    ProductCode = item.Noproduct,
                    ProductNumber = item.Codeproduct,
                    ProductNameTh = item.Productname ?? "DK",
                    ProductNameEn = item.Productname ?? "DK",

                    ImageName = item.NoCode,
                    ImagePath = $"{item.NoCode}.jpg",

                    //Wo = plan.Wo,
                    //WoNumber = plan.WoNumber,

                    Size = item.Ringsize,
                    Remark = item.Remark,

                    CreateBy = CurrentUsername,
                    CreateDate = DateTime.UtcNow
                };

                //check product type
                if (!string.IsNullOrEmpty(item.Typep))
                {
                    var productType = GetProductType(masterProductType, item.Typep);
                    newProduct.ProductType = productType.Code;
                    newProduct.ProductTypeName = productType.NameTh;
                }

                //get production date
                newProduct.ProductionDate = GetProductionDate(item.Dateproduct);

                //get production type
                newProduct.ProductionType = ProducttionType(masterGold, item.Typeg);

                //get production type size
                newProduct.ProductionTypeSize = ProducttionTypeSize(masterGoldSize, item.Productname);

                //get wo
                newProduct.WoOrigin = item.Jobno;
                newProduct.Wo = GetWO(item.Jobno);
                newProduct.WoNumber = GetWONumber(item.Jobno);

                //add product
                addProducts.Add(newProduct);

                //gold
                if (!string.IsNullOrEmpty(item.Typeg))
                {
                    var check = item.Typeg.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.Typeg, item.Typed1, item.Qtyg, item.Wg, item.Unit1, item.Priceg, null);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }
                }


                //check typD
                if (!string.IsNullOrEmpty(item.Typed))
                {
                    var check = item.Typed.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.Typed, item.Typed1, item.Qtyd, item.Wd, item.Unit2, item.Priced, null);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }
                }

                //check typeR
                if (!string.IsNullOrEmpty(item.Typer))
                {
                    var check = item.Typer.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.Typer, item.Typed1, item.Qtyr, item.Wr, item.Unit3, item.Pricer, item.Sizer);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }

                }

                //check typeS
                if (!string.IsNullOrEmpty(item.TypeS))
                {
                    var check = item.TypeS.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.TypeS, item.Typed1, item.Qtys, item.Ws, item.Unit4, item.Prices, item.Sizes);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }

                }

                //check typeE
                if (!string.IsNullOrEmpty(item.Typee))
                {
                    var check = item.Typee.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.Typee, item.Typed1, item.Qtye, item.We, item.Unit5, item.Pricee, item.Sizee);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }

                }

                //check typeM
                if (!string.IsNullOrEmpty(item.Typem))
                {
                    var check = item.Typem.ToUpper().Trim();
                    var newMaterial = GetMaterial(_stockRunning, check, item.Typem, item.Typed1, item.Qtym, item.Wm, item.Unit6, item.Pricem, item.Sizem);

                    if (CheckTypeOrigin(newMaterial.TypeOrigin))
                    {
                        addProductsMaterial.Add(newMaterial);
                    }

                }

                item.Typejob = "convert";
            }

            using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (addProducts.Any())
                {
                    _jewelryContext.TbtStockProduct.AddRange(addProducts);
                    await _jewelryContext.SaveChangesAsync();
                }

                if (addProductsMaterial.Any())
                {
                    _jewelryContext.TbtStockProductMaterial.AddRange(addProductsMaterial);
                    //await _jewelryContext.SaveChangesAsync();
                }

                if (oldProduct.Any())
                {
                    _jewelryContext.StockFromConvert.UpdateRange(oldProduct);
                }

                await _jewelryContext.SaveChangesAsync();

                scope.Complete();
            }

            return $"success {add} item";
        }
        private TbmProductType GetProductType(List<TbmProductType> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "DK") ?? master.FirstOrDefault();
            }

            var checkText = check.ToUpper().Trim();

            // ตรวจสอบแต่ละประเภทตามลำดับความสำคัญ
            if (checkText.Contains("RING"))
            {
                return master.FirstOrDefault(x => x.Code == "R");
            }
            else if (checkText.Contains("PENDANT"))
            {
                return master.FirstOrDefault(x => x.Code == "P");
            }
            else if (checkText.Contains("EARRING") || checkText.Contains("LOCK") || checkText.Contains("STDU") || checkText.Contains("STUD"))
            {
                if (checkText.Contains("STUD") || checkText.Contains("STUD"))
                {
                    return master.FirstOrDefault(x => x.Code == "ES");
                }
                else if (checkText.Contains("HOOK"))
                {
                    return master.FirstOrDefault(x => x.Code == "EH");
                }
                else if (checkText.Contains("LOCK"))
                {
                    return master.FirstOrDefault(x => x.Code == "EL");
                }
                else
                {
                    return master.FirstOrDefault(x => x.Code == "E");
                }
            }
            else if (checkText.Contains("BRACELET"))
            {
                return master.FirstOrDefault(x => x.Code == "B");
            }
            else if (checkText.Contains("NECKLACE"))
            {
                return master.FirstOrDefault(x => x.Code == "N");
            }
            else if (checkText.Contains("BANGLE"))
            {
                return master.FirstOrDefault(x => x.Code == "G");
            }
            else if (checkText.Contains("BROOCH"))
            {
                return master.FirstOrDefault(x => x.Code == "T");
            }
            else if (checkText.Contains("BUTTON"))
            {
                return master.FirstOrDefault(x => x.Code == "V");
            }
            else if (checkText.Contains("CHAIN"))
            {
                return master.FirstOrDefault(x => x.Code == "CH");
            }

            // หากยังไม่พบ ให้ใช้ค่าเริ่มต้น
            return master.FirstOrDefault(x => x.Code == "DK") ?? master.FirstOrDefault();
        }
        private DateTime GetProductionDate(string productionDateCode)
        {
            try
            {
                // ถ้าเป็นค่าว่างหรือ null ให้ใช้วันที่ปัจจุบัน
                if (string.IsNullOrEmpty(productionDateCode) || productionDateCode.ToUpper() == "[NULL]")
                {
                    return DateTime.UtcNow;
                }

                // ทำความสะอาดข้อมูล ตัดอักขระพิเศษหรือช่องว่างออก
                productionDateCode = productionDateCode.Trim();

                // ตรวจสอบว่าเป็นตัวเลขจำนวน 8 หลักหรือไม่
                if (productionDateCode.Length != 8 || !int.TryParse(productionDateCode, out _))
                {
                    return DateTime.UtcNow;
                }

                // แยกปี เดือน วัน
                int yearThai = int.Parse(productionDateCode.Substring(0, 4));  // พ.ศ.
                int month = int.Parse(productionDateCode.Substring(4, 2));      // เดือน
                int day = int.Parse(productionDateCode.Substring(6, 2));        // วัน

                // แปลงปี พ.ศ. เป็น ค.ศ.
                int yearGregorian = yearThai - 543;

                // ตรวจสอบความถูกต้องของวันที่
                if (month < 1 || month > 12 || day < 1 || day > 31 || yearGregorian < 1900 || yearGregorian > 2100)
                {
                    return DateTime.UtcNow;
                }

                // สร้าง DateTime ในเขตเวลาท้องถิ่น (ประเทศไทย - GMT+7)
                var localTime = new DateTime(yearGregorian, month, day, 0, 0, 0, DateTimeKind.Local);

                // แปลงเป็น UTC
                return localTime.ToUniversalTime();
            }
            catch (Exception)
            {
                // กรณีเกิดข้อผิดพลาดในการแปลงค่า ให้ใช้วันที่ปัจจุบัน
                return DateTime.UtcNow;
            }
        }
        private string ProducttionType(List<TbmGold> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "WG").NameEn;
            }

            var checkText = check.ToUpper().Trim();

            if (checkText.Contains("PG"))
            {
                return master.FirstOrDefault(x => x.Code == "PG").NameEn;
            }
            else if (checkText.Contains("YG"))
            {
                return master.FirstOrDefault(x => x.Code == "YG").NameEn;
            }
            else if (checkText.Contains("SI"))
            {
                return master.FirstOrDefault(x => x.Code == "SV").NameEn;
            }
            else
            {
                return master.FirstOrDefault(x => x.Code == "WG").NameEn;
            }
        }
        private string ProducttionTypeSize(List<TbmGoldSize> master, string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                // ถ้าไม่มีข้อมูลประเภทสินค้า ให้ใช้ค่าเริ่มต้น (DK)
                return master.FirstOrDefault(x => x.Code == "100").NameEn;
            }

            var checkText = check.ToUpper().Trim();

            if (checkText.Contains("14K"))
            {
                return master.FirstOrDefault(x => x.Code == "5").NameEn;
            }
            else if (checkText.Contains("18K"))
            {
                return master.FirstOrDefault(x => x.Code == "7").NameEn;
            }
            else if (checkText.Contains("22K"))
            {
                return master.FirstOrDefault(x => x.Code == "8").NameEn;
            }
            else if (checkText.Contains("10K"))
            {
                return master.FirstOrDefault(x => x.Code == "4").NameEn;
            }
            else if (checkText.Contains("9K"))
            {
                return master.FirstOrDefault(x => x.Code == "3").NameEn;
            }
            else if (checkText.Contains("SIL"))
            {
                return master.FirstOrDefault(x => x.Code == "9").NameEn;
            }
            else
            {
                return master.FirstOrDefault(x => x.Code == "100").NameEn;
            }
        }
        private string? GetWO(string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                return null;
            }
            if (check.StartsWith("NO."))
            {
                check = check.Substring(3).Trim();
            }
            if (check.Contains("NO."))
            {
                // หาตำแหน่งเริ่มต้นของ "NO."
                int startPos = check.IndexOf("NO.");
                // ตัด "NO." ออกและคืนค่าข้อความที่เหลือ
                check = check.Substring(startPos + 3).Trim();
            }

            // ตรวจสอบกรณีที่มีทั้ง "-" และ "/"
            bool hasDash = check.Contains("-");
            bool hasSlash = check.Contains("/");

            if (hasSlash && hasDash)
            {
                // กรณีมีทั้ง "-" และ "/" ให้ตัดตาม "/" เท่านั้น
                string[] parts = check.Split('/');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }
            else if (hasSlash)
            {
                // กรณีมีแค่ "/"
                string[] parts = check.Split('/');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }
            else if (hasDash)
            {
                // กรณีมีแค่ "-"
                string[] parts = check.Split('-');
                return parts.Length > 0 ? parts[0].Trim() : check;
            }

            // กรณีไม่พบเครื่องหมายใ
            return check;
        }
        private int? GetWONumber(string check)
        {
            if (string.IsNullOrEmpty(check))
            {
                return null;
            }

            // ตัดช่องว่างหน้า-หลังออก
            check = check.Trim();

            // ถ้าข้อความขึ้นต้นด้วย "NO." ให้ตัดออก
            if (check.StartsWith("NO."))
            {
                check = check.Substring(3).Trim();
            }

            // ถ้าพบคำว่า "NO." ในตำแหน่งอื่น
            if (check.Contains("NO."))
            {
                // หาตำแหน่งเริ่มต้นของ "NO."
                int startPos = check.IndexOf("NO.");
                // ตัด "NO." ออกและคืนค่าข้อความที่เหลือ
                check = check.Substring(startPos + 3).Trim();
            }

            // ตรวจสอบกรณีที่มีทั้ง "-" และ "/"
            bool hasDash = check.Contains("-");
            bool hasSlash = check.Contains("/");

            string secondPart = null;

            if (hasSlash && hasDash)
            {
                // กรณีมีทั้ง "-" และ "/" ให้ตัดตาม "/" เท่านั้น
                string[] parts = check.Split('/');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }
            else if (hasSlash)
            {
                // กรณีมีแค่ "/"
                string[] parts = check.Split('/');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }
            else if (hasDash)
            {
                // กรณีมีแค่ "-"
                string[] parts = check.Split('-');
                if (parts.Length >= 2)
                {
                    secondPart = parts[1].Trim();
                }
            }

            // ถ้าไม่มีตำแหน่งที่ 2 หรือไม่พบเครื่องหมายใดๆ
            if (secondPart == null)
            {
                return null;
            }

            // พยายามแปลงเป็น int
            if (int.TryParse(secondPart, out int result))
            {
                return result;
            }

            return null;
        }

        private TbtStockProductMaterial? GetMaterial(string running, string check, string type, string typeCode, int? qty, int? weight, string weghtUnit, int? price, string size)
        {
            var newMaterial = new TbtStockProductMaterial();

            newMaterial.StockNumber = running;
            newMaterial.CreateDate = DateTime.UtcNow;
            newMaterial.CreateBy = CurrentUsername;

            newMaterial.TypeOrigin = string.IsNullOrEmpty(type) || string.IsNullOrWhiteSpace(type) || type == "[NULL]" || type == " " ? null : type;
            newMaterial.Type = "Gem";
            newMaterial.TypeCode = typeCode;

            newMaterial.Qty = qty.HasValue ? qty.Value : 0;
            //QtyUnit = item.Unit2,

            newMaterial.Weight = weight.HasValue ? weight.Value : 0;
            newMaterial.WeightUnit = weghtUnit;
            newMaterial.Price = price.HasValue ? price.Value : 0;

            if (check.Contains("DIA") || check == "D")
            {
                newMaterial.Type = "Diamond";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.Type}{newMaterial.Weight} {newMaterial.WeightUnit} {newMaterial.TypeCode}";
            }

            if (check.Contains("CZ"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Cubic Zirconia";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("RU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Ruby";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("PINK SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Pink Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("PINK SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Yellow Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("FANCY SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Fancy Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("YELLOW SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Yellow Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("WHITE SAP"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "White Sapphire";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("AME"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Amethyst";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("EME") || check == "E")
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Emerald";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("BLUE TO") || check.Contains("BT"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Blue Topaz";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("GREEN TO"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Green Topaz";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }


            if (check.Contains("CIT") || check.Contains("CL"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Citrine";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("TANZ"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Tanzanite";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("MIX"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "Mix Stone";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("AQU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Aquamarine";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("LEMON"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "Lemon Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("ROSE QU"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Rose Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }
            if (check.Contains("WHITE QU"))
            {
                newMaterial.Type = "GEM";
                newMaterial.TypeCode = "White Quart";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("TSA"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Tsavolite";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("GAR"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "Garnet";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("WHITE PEA"))
            {
                newMaterial.Type = "Gem";
                newMaterial.TypeCode = "WHITE Pearl";
                newMaterial.TypeBarcode = $"{newMaterial.Qty}{newMaterial.TypeCode}{newMaterial.Weight} {newMaterial.WeightUnit}";
            }

            if (check.Contains("GOLD"))
            {
                newMaterial.Type = "Gold";
                newMaterial.TypeCode = check.Contains("WHITE") ? "WG" : "YG";
                newMaterial.TypeBarcode = $"{newMaterial.Weight} {newMaterial.WeightUnit} {newMaterial.Type} {newMaterial.Size ?? null}";
            }

            return newMaterial;
        }


        private bool CheckTypeOrigin(string check)
        {
            var response = false;

            if (!string.IsNullOrEmpty(check))
            {
                response = true;
            }
            if (!string.IsNullOrWhiteSpace(check))
            {
                response = true;
            }

            if (check == " ")
            {
                response = false;
            }
            if (check == "[NULL]")
            {
                response = false;
            }

            return response;
        }
    }
}
