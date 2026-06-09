using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Receipt.Production;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Receipt.Outsource
{
    public class ReceiptOutsourceService : BaseService, IReceiptOutsourceService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IHostEnvironment _hostingEnvironment;
        private readonly IRunningNumber _runningNumberService;

        public ReceiptOutsourceService(
            JewelryContext jewelryContext,
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment hostingEnvironment,
            IRunningNumber runningNumberService)
            : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _hostingEnvironment = hostingEnvironment;
            _runningNumberService = runningNumberService;
        }

        public async Task<jewelry.Model.Receipt.Outsource.Confirm.Response> Confirm(jewelry.Model.Receipt.Outsource.Confirm.Request request)
        {
            var response = new jewelry.Model.Receipt.Outsource.Confirm.Response
            {
                Vendor = request.Vendor,
                PoNumber = request.PoNumber,
                Stocks = new List<jewelry.Model.Receipt.Outsource.Confirm.Stock>()
            };

            var headerRunning = await _runningNumberService.GenerateRunningNumber("OUT");

            var header = new TbtStockProductReceiptPlan
            {
                Running = headerRunning,
                Type = "outsource",
                Vendor = request.Vendor,
                PoNumber = request.PoNumber,
                Qty = request.Stocks.Count,
                QtyRunning = request.Stocks.Count,
                IsComplete = true,
                IsRunning = true,
                Wo = null,
                WoNumber = null,
                WoText = null,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            var newReceiptItems = new List<TbtStockProductReceiptItem>();
            var newStocks = new List<StockProductDto>();
            var newStockPieceMaterials = new List<TbtStockPieceMaterial>();

            foreach (var stock in request.Stocks)
            {
                var productType = await _jewelryContext.TbmProductType
                    .FirstOrDefaultAsync(x => x.Code == stock.ProductType);

                if (productType == null)
                {
                    throw new KeyNotFoundException($"ไม่พบประเภทสินค้า: {stock.ProductType}");
                }

                var prefix = stock.ProductionType == "Silver" ? productType.SilverCode : productType.ProductCode;
                var stockRunning = await _runningNumberService.GenerateRunningNumberForStockProductHash(prefix);

                var newProduct = stock.MapNewStockOutsource(request.Vendor, request.PoNumber, productType.NameTh, stockRunning, CurrentUsername);
                var newProductResponse = newProduct.MapResponseNewStockOutsource();
                newStocks.Add(newProduct);

                var receiptItem = new TbtStockProductReceiptItem
                {
                    StockReceiptNumber = stockRunning,
                    Running = headerRunning,
                    Type = "outsource",
                    IsReceipt = true,
                    StockNumber = stockRunning,
                    ReceiptDate = DateTime.UtcNow,
                    Vendor = request.Vendor,
                    Po = request.PoNumber,
                    ProductType = stock.ProductType,
                    ProductTypeName = productType.NameTh,
                    ProductionType = stock.ProductionType,
                    ProductionTypeSize = stock.ProductionTypeSize,
                    MoldDesign = stock.MoldDesign,
                    Wo = null,
                    WoNumber = null,
                    WoText = null,
                    CreateDate = DateTime.UtcNow,
                    CreateBy = CurrentUsername
                };
                newReceiptItems.Add(receiptItem);

                if (stock.Materials != null && stock.Materials.Any())
                {
                    var pieceProductCode = stock.ProductNumber.ToUpper();
                    var materials = stock.MapNewOutsourcePieceMaterial(stockRunning, pieceProductCode, CurrentUsername);
                    newStockPieceMaterials.AddRange(materials);

                    newProductResponse.Materials = materials.Select(m => new jewelry.Model.Receipt.Outsource.Confirm.Material
                    {
                        Type = m.Type,
                        TypeName = m.TypeName,
                        TypeCode = m.TypeCode,
                        TypeBarcode = m.TypeBarcode,
                        Qty = m.Qty,
                        QtyUnit = m.QtyUnit,
                        Weight = m.Weight,
                        WeightUnit = m.WeightUnit,
                        Size = m.Size,
                        Region = m.Region,
                        Price = m.Price
                    }).ToList();
                }

                response.Stocks.Add(newProductResponse);
            }

            var newSkus = new List<TbtSku>();
            var newStockPieces = new List<TbtStockPiece>();
            var upsertBalances = new List<TbtStockBalance>();
            var newMovements = new List<TbtStockMovement>();

            var skuCache = new Dictionary<string, bool>();
            var locationCache = new Dictionary<string, string>();
            var balanceCache = new Dictionary<string, TbtStockBalance>();

            foreach (var stock in newStocks)
            {
                var skuCode = stock.DeriveSkuCode();

                if (!skuCache.ContainsKey(skuCode))
                {
                    var skuExists = await _jewelryContext.TbtSku.AnyAsync(x => x.SkuCode == skuCode);
                    if (!skuExists)
                    {
                        newSkus.Add(stock.MapNewSku(skuCode, CurrentUsername));
                    }
                    skuCache[skuCode] = true;
                }

                var locationCode = await ReceiptProductionServiceExtention.ResolveLocationCodeAsync(_jewelryContext, stock.Location, CurrentUsername, locationCache);
                var pieceProductCode = stock.ProductNumber ?? stock.StockNumber;

                newStockPieces.Add(stock.MapNewStockPiece(skuCode, locationCode, CurrentUsername));

                var balanceKey = $"{skuCode}|{locationCode}";
                if (balanceCache.ContainsKey(balanceKey))
                {
                    balanceCache[balanceKey].QtyOnHand += 1;
                    balanceCache[balanceKey].LastMovementAt = DateTime.UtcNow;
                }
                else
                {
                    var existingBalance = await _jewelryContext.TbtStockBalance
                        .FirstOrDefaultAsync(x => x.SkuCode == skuCode && x.LocationCode == locationCode);

                    if (existingBalance != null)
                    {
                        existingBalance.QtyOnHand += 1;
                        existingBalance.LastMovementAt = DateTime.UtcNow;
                        balanceCache[balanceKey] = existingBalance;
                    }
                    else
                    {
                        var newBalance = new TbtStockBalance
                        {
                            SkuCode = skuCode,
                            LocationCode = locationCode,
                            QtyOnHand = 1,
                            QtyReserved = 0,
                            LastMovementAt = DateTime.UtcNow,
                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow
                        };
                        balanceCache[balanceKey] = newBalance;
                        upsertBalances.Add(newBalance);
                    }
                }

                newMovements.Add(ReceiptProductionServiceExtention.MapNewReceiptMovement(skuCode, stock.StockNumber, pieceProductCode, locationCode, stock.ReceiptNumber, CurrentUsername));
            }

            var updateBalances = balanceCache.Values
                .Where(x => x.Id != 0)
                .ToList();

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                _jewelryContext.TbtStockProductReceiptPlan.Add(header);

                if (newReceiptItems.Any())
                {
                    _jewelryContext.TbtStockProductReceiptItem.AddRange(newReceiptItems);
                }
                if (newSkus.Any())
                {
                    _jewelryContext.TbtSku.AddRange(newSkus);
                }
                if (newStockPieces.Any())
                {
                    _jewelryContext.TbtStockPiece.AddRange(newStockPieces);
                }
                if (newStockPieceMaterials.Any())
                {
                    _jewelryContext.TbtStockPieceMaterial.AddRange(newStockPieceMaterials);
                }
                if (upsertBalances.Any())
                {
                    _jewelryContext.TbtStockBalance.AddRange(upsertBalances);
                }
                if (updateBalances.Any())
                {
                    _jewelryContext.TbtStockBalance.UpdateRange(updateBalances);
                }
                if (newMovements.Any())
                {
                    _jewelryContext.TbtStockMovement.AddRange(newMovements);
                }

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return response;
        }

        public IQueryable<jewelry.Model.Receipt.Outsource.History.List.Response> ListHistory(jewelry.Model.Receipt.Outsource.History.List.Search request)
        {
            var receipt = (from item in _jewelryContext.TbtStockProductReceiptItem
                           where item.IsReceipt == true && item.Type == "outsource"
                           select item);

            if (request.ReceiptDateStart.HasValue)
            {
                receipt = receipt.Where(x => x.ReceiptDate >= request.ReceiptDateStart.Value.StartOfDayUtc());
            }
            if (request.ReceiptDateEnd.HasValue)
            {
                receipt = receipt.Where(x => x.ReceiptDate <= request.ReceiptDateEnd.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.StockNumber))
            {
                receipt = receipt.Where(x => x.StockNumber.Contains(request.StockNumber));
            }
            if (!string.IsNullOrEmpty(request.Mold))
            {
                receipt = receipt.Where(x => x.MoldDesign.Contains(request.Mold));
            }
            if (request.ProductType != null && request.ProductType.Any())
            {
                var productTypeArray = request.ProductType.Select(x => x).ToArray();
                receipt = receipt.Where(x => productTypeArray.Contains(x.ProductType));
            }
            if (!string.IsNullOrEmpty(request.Vendor))
            {
                receipt = receipt.Where(x => x.Vendor.Contains(request.Vendor));
            }
            if (!string.IsNullOrEmpty(request.PoNumber))
            {
                receipt = receipt.Where(x => x.Po.Contains(request.PoNumber));
            }

            if (!receipt.Any())
            {
                return Enumerable.Empty<jewelry.Model.Receipt.Outsource.History.List.Response>().AsQueryable();
            }

            var query = (from item in receipt

                         join piece in _jewelryContext.TbtStockPiece
                         on item.StockNumber equals piece.StockNumber

                         join sku in _jewelryContext.TbtSku
                         on piece.SkuCode equals sku.SkuCode

                         select new jewelry.Model.Receipt.Outsource.History.List.Response
                         {
                             StockNumber = piece.StockNumber,
                             Status = piece.Status,

                             ReceiptNumber = item.StockReceiptNumber,
                             ReceiptDate = piece.ReceiptDate.GetValueOrDefault(),
                             ReceiptType = item.Type,

                             Mold = item.MoldDesign,
                             MoldDesign = item.MoldDesign,

                             Qty = 1,
                             ProductPrice = sku.DefaultPrice ?? 0,

                             ProductNumber = sku.ProductNumber,
                             ProductNameTh = sku.ProductNameTh,
                             ProductNameEn = sku.ProductNameEn,

                             ProductType = sku.ProductType,
                             ProductTypeName = item.ProductTypeName,

                             ImageName = sku.ImageName,
                             ImagePath = sku.ImagePath,

                             Vendor = piece.Vendor,
                             PoNumber = piece.PoNumber,

                             ProductionDate = item.CreateDate,
                             ProductionType = item.ProductionType,
                             ProductionTypeSize = item.ProductionTypeSize,

                             Size = sku.Size,
                             Location = piece.LocationCode,
                             Remark = piece.Remark,

                             StudEarring = sku.StudEarring,

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
    }
}
