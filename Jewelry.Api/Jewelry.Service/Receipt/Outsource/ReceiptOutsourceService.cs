using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Receipt.Production;
using Jewelry.Service.Stock;
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

            var goldRows = await _jewelryContext.TbmGold.AsNoTracking().ToListAsync();
            var goldNameByCode = goldRows
                .GroupBy(x => x.Code.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First().NameEn);
            var goldSizeRows = await _jewelryContext.TbmGoldSize.AsNoTracking().ToListAsync();
            var goldSizeNameByCode = goldSizeRows
                .GroupBy(x => x.Code.Trim().ToUpper())
                .ToDictionary(g => g.Key, g => g.First().NameEn);

            var locationCache = new Dictionary<string, string>();
            var topUpRows = new List<TopUpRow>();

            // ตรวจ qty + เตรียมแถวเติมล็อตก่อนออกเลขวิ่ง (กันเลขวิ่งเปลืองเมื่อข้อมูลไม่ผ่าน)
            for (var i = 0; i < request.Stocks.Count; i++)
            {
                var row = request.Stocks[i];

                // UI ส่ง master code (WG/7) มา แต่ทุก path รับสินค้าอื่นเก็บเป็นชื่อ (White Gold/18K) จึงแปลงให้ตรงกันก่อนใช้งาน
                row.ProductionType = NormalizeToName(row.ProductionType, goldNameByCode);
                row.ProductionTypeSize = NormalizeToName(row.ProductionTypeSize, goldSizeNameByCode);

                var rowIsSilver = row.ProductionType == "Silver";
                if (rowIsSilver)
                {
                    if (row.Qty <= 0)
                    {
                        throw new HandleException($"จำนวนต้องมากกว่า 0 --> {row.ProductNumber}");
                    }
                    if (row.Qty != decimal.Truncate(row.Qty))
                    {
                        throw new HandleException($"เงินต้องระบุจำนวนเป็นจำนวนเต็ม --> {row.ProductNumber} (Qty = {row.Qty})");
                    }
                }
                else if (row.Qty != 1)
                {
                    throw new HandleException($"ทอง 1 รหัส = 1 ชิ้น กรุณาเพิ่มรายการใหม่ --> {row.ProductNumber}");
                }

                var topUpNumber = ReceiptTopUpHelper.CleanStockNumber(row.TopUpStockNumber);
                if (topUpNumber != null)
                {
                    topUpRows.Add(new TopUpRow
                    {
                        StockNumber = topUpNumber,
                        IsSilver = rowIsSilver,
                        Location = row.Location,
                        Qty = row.Qty,
                        ReceiptNumber = string.Empty
                    });
                }
            }

            await ReceiptTopUpHelper.LoadAndValidateAsync(_jewelryContext, topUpRows, locationCache, CurrentUsername, track: false);
            topUpRows.Clear();

            var totalPieces = (int)request.Stocks.Sum(x => x.Qty);

            var headerRunning = await _runningNumberService.GenerateRunningNumber("OUT");

            var header = new TbtStockProductReceiptPlan
            {
                Running = headerRunning,
                Type = "outsource",
                Vendor = request.Vendor,
                PoNumber = request.PoNumber,
                Qty = totalPieces,
                QtyRunning = totalPieces,
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

            for (var rowIndex = 0; rowIndex < request.Stocks.Count; rowIndex++)
            {
                var stock = request.Stocks[rowIndex];
                var topUpNumber = ReceiptTopUpHelper.CleanStockNumber(stock.TopUpStockNumber);

                var productType = await _jewelryContext.TbmProductType
                    .FirstOrDefaultAsync(x => x.Code == stock.ProductType);

                if (productType == null)
                {
                    throw new KeyNotFoundException($"ไม่พบประเภทสินค้า: {stock.ProductType}");
                }

                string stockRunning;
                string receiptNumber;
                if (topUpNumber != null)
                {
                    stockRunning = topUpNumber;
                    receiptNumber = $"{headerRunning}-{rowIndex + 1}";
                }
                else
                {
                    var prefix = stock.ProductionType == "Silver" ? productType.SilverCode : productType.ProductCode;
                    stockRunning = await _runningNumberService.GenerateRunningNumberForStockProductHash(prefix);
                    receiptNumber = stockRunning;
                }

                var newProduct = stock.MapNewStockOutsource(request.Vendor, request.PoNumber, productType.NameTh, stockRunning, CurrentUsername);
                newProduct.ReceiptNumber = receiptNumber;
                var newProductResponse = newProduct.MapResponseNewStockOutsource();

                if (topUpNumber != null)
                {
                    newProductResponse.IsTopUp = true;
                    topUpRows.Add(new TopUpRow
                    {
                        StockNumber = topUpNumber,
                        IsSilver = stock.ProductionType == "Silver",
                        Location = stock.Location,
                        Qty = stock.Qty,
                        ReceiptNumber = receiptNumber
                    });
                }
                else
                {
                    newStocks.Add(newProduct);
                }

                var receiptItem = new TbtStockProductReceiptItem
                {
                    StockReceiptNumber = receiptNumber,
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

                if (topUpNumber == null && stock.Materials != null && stock.Materials.Any())
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
            var balanceCache = new Dictionary<string, TbtStockBalance>();

            // เคารพ transaction เดิมถ้าถูกเรียกจากภายในอีก transaction ที่เปิดอยู่แล้ว เหมือน InvoiceService.Create / StockMovementService.MoveLocation
            var ownsTransaction = _jewelryContext.Database.CurrentTransaction == null;
            var transaction = ownsTransaction
                ? await _jewelryContext.Database.BeginTransactionAsync()
                : null;

            try
            {

            // ลำดับล็อก: piece ของล็อตที่เติม -> balance (ล็อตใหม่ไม่มีแถว piece ให้ล็อก แต่ balance ของ SKU ชนกับ path อื่นได้)
            await _jewelryContext.LockStockPiecesAsync(topUpRows.Select(x => x.StockNumber));
            var topUpPieces = await ReceiptTopUpHelper.LoadAndValidateAsync(_jewelryContext, topUpRows, locationCache, CurrentUsername, track: true);

            var skuCodesToLock = newStocks.Select(x => x.DeriveSkuCode())
                .Concat(topUpPieces.Values.Select(x => x.SkuCode))
                .Distinct()
                .ToList();
            await _jewelryContext.LockStockBalancesAsync(skuCodesToLock);

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

                var lotQty = stock.Qty;
                newStockPieces.Add(stock.MapNewStockPiece(skuCode, locationCode, CurrentUsername, qty: lotQty));

                var balanceKey = $"{skuCode}|{locationCode}";
                if (balanceCache.ContainsKey(balanceKey))
                {
                    balanceCache[balanceKey].QtyOnHand += lotQty;
                    balanceCache[balanceKey].LastMovementAt = DateTime.UtcNow;
                }
                else
                {
                    var existingBalance = await _jewelryContext.TbtStockBalance
                        .FirstOrDefaultAsync(x => x.SkuCode == skuCode && x.LocationCode == locationCode);

                    if (existingBalance != null)
                    {
                        existingBalance.QtyOnHand += lotQty;
                        existingBalance.LastMovementAt = DateTime.UtcNow;
                        balanceCache[balanceKey] = existingBalance;
                    }
                    else
                    {
                        var newBalance = new TbtStockBalance
                        {
                            SkuCode = skuCode,
                            LocationCode = locationCode,
                            QtyOnHand = lotQty,
                            QtyReserved = 0,
                            LastMovementAt = DateTime.UtcNow,
                            CreateBy = CurrentUsername,
                            CreateDate = DateTime.UtcNow
                        };
                        balanceCache[balanceKey] = newBalance;
                        upsertBalances.Add(newBalance);
                    }
                }

                newMovements.Add(ReceiptProductionServiceExtention.MapNewReceiptMovement(skuCode, stock.StockNumber, pieceProductCode, locationCode, stock.ReceiptNumber, CurrentUsername, qty: lotQty));
            }

            await ReceiptTopUpHelper.ApplyAsync(_jewelryContext, topUpRows, topUpPieces, balanceCache, upsertBalances, newMovements, CurrentUsername);

            var updateBalances = balanceCache.Values
                .Where(x => x.Id != 0)
                .ToList();

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
            if (topUpPieces.Any())
            {
                _jewelryContext.TbtStockPiece.UpdateRange(topUpPieces.Values);
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

            if (ownsTransaction)
            {
                await transaction!.CommitAsync();
            }

            return response;
            }
            catch
            {
                if (ownsTransaction)
                {
                    await transaction!.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (ownsTransaction)
                {
                    await transaction!.DisposeAsync();
                }
            }
        }

        private static string? NormalizeToName(string? value, Dictionary<string, string> nameByCode)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return nameByCode.TryGetValue(value.Trim().ToUpper(), out var name) ? name : value;
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
                var normalized = StockNumberSearch.Normalize(request.StockNumber);
                if (!string.IsNullOrEmpty(normalized))
                {
                    receipt = receipt.Where(x => x.StockNumber != null && x.StockNumber.Replace("-", "").Contains(normalized));
                }
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

                         let receivedQty = _jewelryContext.TbtStockMovement
                             .Where(m => m.RefDocType == "RECEIPT" && m.RefDocNo == item.StockReceiptNumber && m.StockNumber == item.StockNumber)
                             .Sum(m => (decimal?)m.Qty)

                         select new jewelry.Model.Receipt.Outsource.History.List.Response
                         {
                             StockNumber = piece.StockNumber,
                             Status = piece.Status,

                             ReceiptNumber = item.StockReceiptNumber,
                             ReceiptDate = item.ReceiptDate.GetValueOrDefault(),
                             ReceiptType = item.Type,

                             Mold = item.MoldDesign,
                             MoldDesign = item.MoldDesign,

                             Qty = receivedQty ?? 1,
                             IsTopUp = piece.ReceiptNumber != item.StockReceiptNumber,
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

        public void FillHistoryMaterials(IEnumerable<jewelry.Model.Receipt.Outsource.History.List.Response> rows)
        {
            ReceiptHistoryMaterialHelper.Enrich(_jewelryContext, rows, x => x.StockNumber);
        }

        public jewelry.Model.Receipt.ReceiptHistorySummary GetHistorySummary(jewelry.Model.Receipt.Outsource.History.List.Search request)
        {
            var query = ListHistory(request);
            return ReceiptHistoryMaterialHelper.Summarize(_jewelryContext, query.Select(x => x.StockNumber), query.Select(x => x.Qty), query.Select(x => x.ProductPrice * x.Qty));
        }
    }
}
