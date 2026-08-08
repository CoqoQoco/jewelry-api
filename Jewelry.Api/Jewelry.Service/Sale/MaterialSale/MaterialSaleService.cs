using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace Jewelry.Service.Sale.MaterialSale
{
    public class MaterialSaleService : BaseService, IMaterialSaleService
    {
        private readonly JewelryContext _jewelryContext;
        private readonly IRunningNumber _runningNumberService;

        public MaterialSaleService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
            IRunningNumber runningNumberService) : base(jewelryContext, httpContextAccessor)
        {
            _jewelryContext = jewelryContext;
            _runningNumberService = runningNumberService;
        }

        public async Task<string> GenerateDocumentNumber()
        {
            return await _runningNumberService.GenerateRunningNumberForGold("SM");
        }

        private static void ValidateRequest(string? customerName, List<jewelry.Model.Sale.MaterialSale.Create.Item> items, decimal vatPercent, DateTimeOffset documentDate)
        {
            if (documentDate == default(DateTimeOffset) || documentDate.Year < 2000)
            {
                throw new HandleException("กรุณาระบุวันที่เอกสารให้ถูกต้อง");
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                throw new HandleException("กรุณาระบุชื่อลูกค้า");
            }

            if (items == null || items.Count == 0)
            {
                throw new HandleException("กรุณาเพิ่มรายการขายอย่างน้อย 1 รายการ");
            }

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.GemCode))
                {
                    throw new HandleException($"รายการที่ {item.ItemNo}: กรุณาระบุรหัสวัตถุดิบ");
                }

                if (item.QtyWeight <= 0)
                {
                    throw new HandleException($"รายการที่ {item.ItemNo}: น้ำหนัก (กะรัต) ต้องมากกว่า 0");
                }

                if (item.PriceInclVat <= 0)
                {
                    throw new HandleException($"รายการที่ {item.ItemNo}: ราคารวม Vat ต้องมากกว่า 0");
                }

                if (item.QtyPiece < 0)
                {
                    throw new HandleException($"รายการที่ {item.ItemNo}: จำนวนเม็ดต้องไม่ติดลบ");
                }
            }

            if (vatPercent < 0 || vatPercent > 100)
            {
                throw new HandleException("VAT % ไม่ถูกต้อง");
            }
        }

        private static List<TbtSaleMaterialItem> BuildItems(List<jewelry.Model.Sale.MaterialSale.Create.Item> requestItems)
        {
            var items = new List<TbtSaleMaterialItem>();

            foreach (var i in requestItems)
            {
                var priceExclVat = Math.Round(i.PriceInclVat / 1.07m, 2, MidpointRounding.AwayFromZero);
                var amount = Math.Round(priceExclVat * i.QtyWeight, 2, MidpointRounding.AwayFromZero);

                items.Add(new TbtSaleMaterialItem
                {
                    ItemNo = i.ItemNo,
                    GemCode = i.GemCode,
                    GemName = i.GemName,
                    GemGroup = i.GemGroup,
                    GemShape = i.GemShape,
                    GemSize = i.GemSize,
                    GemGrade = i.GemGrade,
                    Description = i.Description,

                    QtyPiece = i.QtyPiece,
                    QtyWeight = i.QtyWeight,
                    PriceInclVat = i.PriceInclVat,
                    PriceExclVat = priceExclVat,
                    Amount = amount,
                    RefStockPrice = i.RefStockPrice,

                    Remark = i.Remark
                });
            }

            return items;
        }

        public async Task<jewelry.Model.Sale.MaterialSale.Create.Response> Create(jewelry.Model.Sale.MaterialSale.Create.Request request)
        {
            ValidateRequest(request.CustomerName, request.Items, request.VatPercent, request.DocumentDate);

            var running = await _runningNumberService.GenerateRunningNumberForGold("SM");
            var documentNo = string.IsNullOrWhiteSpace(request.DocumentNo) ? running : request.DocumentNo;

            var isDuplicate = await _jewelryContext.TbtSaleMaterialHeader
                .AnyAsync(x => x.DocumentNo == documentNo && x.IsDelete == false);
            if (isDuplicate)
            {
                throw new HandleException($"เลขที่เอกสาร {documentNo} ซ้ำในระบบ");
            }

            var items = BuildItems(request.Items);
            var subTotal = Math.Round(items.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero);
            var vatAmount = Math.Round(subTotal * request.VatPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var grandTotal = subTotal + vatAmount;

            var header = new TbtSaleMaterialHeader
            {
                Running = running,
                DocumentNo = documentNo,
                DocumentDate = request.DocumentDate.UtcDateTime,

                CustomerCode = request.CustomerCode,
                CustomerName = request.CustomerName!.Trim(),
                CustomerAddress = request.CustomerAddress,
                CustomerTel = request.CustomerTel,
                CustomerEmail = request.CustomerEmail,
                CustomerTaxId = request.CustomerTaxId,

                SubTotal = subTotal,
                VatPercent = request.VatPercent,
                VatAmount = vatAmount,
                GrandTotal = grandTotal,

                Remark = request.Remark,

                Status = 10,
                StatusName = "Draft",

                IsDelete = false,

                CreateBy = CurrentUsername,
                CreateDate = DateTime.UtcNow
            };

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                _jewelryContext.TbtSaleMaterialHeader.Add(header);

                foreach (var item in items)
                {
                    item.Running = running;
                }
                _jewelryContext.TbtSaleMaterialItem.AddRange(items);

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return new jewelry.Model.Sale.MaterialSale.Create.Response
            {
                Running = running,
                DocumentNo = documentNo
            };
        }

        public async Task<string> Update(jewelry.Model.Sale.MaterialSale.Update.Request request)
        {
            var header = await _jewelryContext.TbtSaleMaterialHeader
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร {request.Running}");
            }

            if (header.Status != 10)
            {
                throw new HandleException("แก้ไขได้เฉพาะเอกสารสถานะร่าง");
            }

            ValidateRequest(request.CustomerName, request.Items, request.VatPercent, request.DocumentDate);

            var documentNo = string.IsNullOrWhiteSpace(request.DocumentNo) ? header.DocumentNo : request.DocumentNo;

            var isDuplicate = await _jewelryContext.TbtSaleMaterialHeader
                .AnyAsync(x => x.DocumentNo == documentNo && x.IsDelete == false && x.Running != request.Running);
            if (isDuplicate)
            {
                throw new HandleException($"เลขที่เอกสาร {documentNo} ซ้ำในระบบ");
            }

            var items = BuildItems(request.Items);
            var subTotal = Math.Round(items.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero);
            var vatAmount = Math.Round(subTotal * request.VatPercent / 100m, 2, MidpointRounding.AwayFromZero);
            var grandTotal = subTotal + vatAmount;

            header.DocumentNo = documentNo;
            header.DocumentDate = request.DocumentDate.UtcDateTime;

            header.CustomerCode = request.CustomerCode;
            header.CustomerName = request.CustomerName!.Trim();
            header.CustomerAddress = request.CustomerAddress;
            header.CustomerTel = request.CustomerTel;
            header.CustomerEmail = request.CustomerEmail;
            header.CustomerTaxId = request.CustomerTaxId;

            header.SubTotal = subTotal;
            header.VatPercent = request.VatPercent;
            header.VatAmount = vatAmount;
            header.GrandTotal = grandTotal;

            header.Remark = request.Remark;

            header.UpdateBy = CurrentUsername;
            header.UpdateDate = DateTime.UtcNow;

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var existingItems = await _jewelryContext.TbtSaleMaterialItem
                    .Where(x => x.Running == request.Running)
                    .ToListAsync();
                _jewelryContext.TbtSaleMaterialItem.RemoveRange(existingItems);

                foreach (var item in items)
                {
                    item.Running = request.Running;
                }
                _jewelryContext.TbtSaleMaterialItem.AddRange(items);

                _jewelryContext.TbtSaleMaterialHeader.Update(header);
                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return header.Running;
        }

        public async Task<jewelry.Model.Sale.MaterialSale.Get.Response> Get(jewelry.Model.Sale.MaterialSale.Get.Request request)
        {
            var header = await _jewelryContext.TbtSaleMaterialHeader
                .Include(x => x.TbtSaleMaterialItem)
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร {request.Running}");
            }

            return new jewelry.Model.Sale.MaterialSale.Get.Response
            {
                Running = header.Running,
                DocumentNo = header.DocumentNo,
                DocumentDate = header.DocumentDate,

                CustomerCode = header.CustomerCode,
                CustomerName = header.CustomerName,
                CustomerAddress = header.CustomerAddress,
                CustomerTel = header.CustomerTel,
                CustomerEmail = header.CustomerEmail,
                CustomerTaxId = header.CustomerTaxId,

                SubTotal = header.SubTotal,
                VatPercent = header.VatPercent,
                VatAmount = header.VatAmount,
                GrandTotal = header.GrandTotal,

                Remark = header.Remark,

                Status = header.Status,
                StatusName = header.StatusName,

                ConfirmDate = header.ConfirmDate,
                ConfirmBy = header.ConfirmBy,
                CancelDate = header.CancelDate,
                CancelBy = header.CancelBy,
                CancelReason = header.CancelReason,

                CreateDate = header.CreateDate,
                CreateBy = header.CreateBy,
                UpdateDate = header.UpdateDate,
                UpdateBy = header.UpdateBy,

                Items = header.TbtSaleMaterialItem
                    .OrderBy(x => x.ItemNo)
                    .Select(x => new jewelry.Model.Sale.MaterialSale.Get.Item
                    {
                        Id = x.Id,
                        ItemNo = x.ItemNo,
                        GemCode = x.GemCode,
                        GemName = x.GemName,
                        GemGroup = x.GemGroup,
                        GemShape = x.GemShape,
                        GemSize = x.GemSize,
                        GemGrade = x.GemGrade,
                        Description = x.Description,
                        QtyPiece = x.QtyPiece,
                        QtyWeight = x.QtyWeight,
                        PriceInclVat = x.PriceInclVat,
                        PriceExclVat = x.PriceExclVat,
                        Amount = x.Amount,
                        RefStockPrice = x.RefStockPrice,
                        Remark = x.Remark
                    }).ToList()
            };
        }

        public IQueryable<jewelry.Model.Sale.MaterialSale.List.Response> List(jewelry.Model.Sale.MaterialSale.List.Request request)
        {
            var query = _jewelryContext.TbtSaleMaterialHeader.Where(x => x.IsDelete == false);

            if (!string.IsNullOrEmpty(request.DocumentNo))
            {
                query = query.Where(x => x.DocumentNo.Contains(request.DocumentNo));
            }

            if (!string.IsNullOrEmpty(request.CustomerName))
            {
                query = query.Where(x => x.CustomerName.Contains(request.CustomerName));
            }

            if (request.Status != null && request.Status.Any())
            {
                query = query.Where(x => request.Status.Contains(x.Status));
            }

            if (request.DocumentDateStart.HasValue)
            {
                query = query.Where(x => x.DocumentDate >= request.DocumentDateStart.Value.StartOfDayUtc());
            }

            if (request.DocumentDateEnd.HasValue)
            {
                query = query.Where(x => x.DocumentDate <= request.DocumentDateEnd.Value.EndOfDayUtc());
            }

            if (!string.IsNullOrEmpty(request.CreateBy))
            {
                query = query.Where(x => x.CreateBy.Contains(request.CreateBy));
            }

            var result = from header in query
                         select new jewelry.Model.Sale.MaterialSale.List.Response
                         {
                             Running = header.Running,
                             DocumentNo = header.DocumentNo,
                             DocumentDate = header.DocumentDate,
                             CustomerName = header.CustomerName,

                             ItemCount = header.TbtSaleMaterialItem.Count,
                             TotalWeight = header.TbtSaleMaterialItem.Sum(x => (decimal?)x.QtyWeight) ?? 0,
                             GrandTotal = header.GrandTotal,

                             Status = header.Status,
                             StatusName = header.StatusName,

                             CreateDate = header.CreateDate,
                             CreateBy = header.CreateBy
                         };

            return result.OrderByDescending(x => x.CreateDate);
        }

        public async Task<string> Confirm(jewelry.Model.Sale.MaterialSale.Confirm.Request request)
        {
            var header = await _jewelryContext.TbtSaleMaterialHeader
                .Include(x => x.TbtSaleMaterialItem)
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร {request.Running}");
            }

            if (header.Status != 10)
            {
                throw new HandleException("ยืนยันได้เฉพาะเอกสารสถานะร่าง");
            }

            if (header.TbtSaleMaterialItem == null || !header.TbtSaleMaterialItem.Any())
            {
                throw new HandleException("เอกสารไม่มีรายการขาย ไม่สามารถยืนยันได้");
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var gemCodes = header.TbtSaleMaterialItem.Select(x => x.GemCode).Distinct().ToList();
                var gems = await _jewelryContext.TbtStockGem
                    .Where(x => gemCodes.Contains(x.Code))
                    .ToListAsync();

                var groupedItems = header.TbtSaleMaterialItem.GroupBy(x => x.GemCode).ToList();

                foreach (var group in groupedItems)
                {
                    var gem = gems.FirstOrDefault(x => x.Code == group.Key);
                    if (gem == null)
                    {
                        throw new HandleException($"{group.Key} --> ไม่พบวัตถุดิบในคลัง");
                    }

                    var sumQtyPiece = group.Sum(x => x.QtyPiece);
                    var sumQtyWeight = group.Sum(x => x.QtyWeight);

                    if (sumQtyPiece > gem.Quantity)
                    {
                        throw new HandleException($"{group.Key} --> จำนวนคงเหลือไม่เพียงพอ");
                    }
                    if (sumQtyWeight > gem.QuantityWeight)
                    {
                        throw new HandleException($"{group.Key} --> น้ำหนักคงเหลือไม่เพียงพอ");
                    }
                }

                var updateGems = new List<TbtStockGem>();
                var newTransections = new List<TbtStockGemTransection>();
                var runningNo = await _runningNumberService.GenerateRunningNumberForGold("SMO");

                foreach (var item in header.TbtSaleMaterialItem.OrderBy(x => x.ItemNo))
                {
                    var gem = gems.First(x => x.Code == item.GemCode);

                    var previousQty = gem.Quantity;
                    var previousQtyWeight = gem.QuantityWeight;

                    gem.Quantity -= item.QtyPiece;
                    gem.QuantityWeight -= item.QtyWeight;

                    gem.UpdateDate = DateTime.UtcNow;
                    gem.UpdateBy = CurrentUsername;

                    if (!updateGems.Contains(gem))
                    {
                        updateGems.Add(gem);
                    }

                    newTransections.Add(new TbtStockGemTransection
                    {
                        Running = runningNo,
                        Code = item.GemCode,
                        Type = 8,

                        Qty = item.QtyPiece,
                        QtyWeight = item.QtyWeight,

                        PreviousRemainQty = previousQty,
                        PreviousRemianQtyWeight = previousQtyWeight,
                        PointRemianQty = gem.Quantity,
                        PointRemianQtyWeight = gem.QuantityWeight,

                        Stastus = "completed",
                        Remark1 = $"ขายวัตถุดิบ {header.DocumentNo}",
                        Remark2 = item.Description,

                        RequestDate = header.DocumentDate,
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow
                    });
                }

                _jewelryContext.TbtStockGem.UpdateRange(updateGems);
                _jewelryContext.TbtStockGemTransection.AddRange(newTransections);

                header.Status = 100;
                header.StatusName = "Confirmed";
                header.ConfirmDate = DateTime.UtcNow;
                header.ConfirmBy = CurrentUsername;
                _jewelryContext.TbtSaleMaterialHeader.Update(header);

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return $"ยืนยันการขายวัตถุดิบ {header.DocumentNo} สำเร็จ";
        }

        public async Task<string> Cancel(jewelry.Model.Sale.MaterialSale.Cancel.Request request)
        {
            var header = await _jewelryContext.TbtSaleMaterialHeader
                .Include(x => x.TbtSaleMaterialItem)
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร {request.Running}");
            }

            if (header.Status != 100)
            {
                throw new HandleException("ยกเลิกได้เฉพาะเอกสารสถานะยืนยันแล้ว");
            }

            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var gemCodes = header.TbtSaleMaterialItem.Select(x => x.GemCode).Distinct().ToList();
                var gems = await _jewelryContext.TbtStockGem
                    .Where(x => gemCodes.Contains(x.Code))
                    .ToListAsync();

                var updateGems = new List<TbtStockGem>();
                var newTransections = new List<TbtStockGemTransection>();
                var runningNo = await _runningNumberService.GenerateRunningNumberForGold("SMI");

                foreach (var item in header.TbtSaleMaterialItem.OrderBy(x => x.ItemNo))
                {
                    var gem = gems.FirstOrDefault(x => x.Code == item.GemCode);
                    if (gem == null)
                    {
                        throw new HandleException($"{item.GemCode} --> ไม่พบวัตถุดิบในคลัง");
                    }

                    var previousQty = gem.Quantity;
                    var previousQtyWeight = gem.QuantityWeight;

                    gem.Quantity += item.QtyPiece;
                    gem.QuantityWeight += item.QtyWeight;

                    gem.UpdateDate = DateTime.UtcNow;
                    gem.UpdateBy = CurrentUsername;

                    if (!updateGems.Contains(gem))
                    {
                        updateGems.Add(gem);
                    }

                    newTransections.Add(new TbtStockGemTransection
                    {
                        Running = runningNo,
                        Code = item.GemCode,
                        Type = 9,

                        Qty = item.QtyPiece,
                        QtyWeight = item.QtyWeight,

                        PreviousRemainQty = previousQty,
                        PreviousRemianQtyWeight = previousQtyWeight,
                        PointRemianQty = gem.Quantity,
                        PointRemianQtyWeight = gem.QuantityWeight,

                        Stastus = "completed",
                        Remark1 = $"ยกเลิกการขายวัตถุดิบ {header.DocumentNo}",
                        Remark2 = item.Description,

                        RequestDate = header.DocumentDate,
                        CreateBy = CurrentUsername,
                        CreateDate = DateTime.UtcNow
                    });
                }

                _jewelryContext.TbtStockGem.UpdateRange(updateGems);
                _jewelryContext.TbtStockGemTransection.AddRange(newTransections);

                header.Status = 500;
                header.StatusName = "Cancelled";
                header.CancelDate = DateTime.UtcNow;
                header.CancelBy = CurrentUsername;
                header.CancelReason = request.CancelReason;
                _jewelryContext.TbtSaleMaterialHeader.Update(header);

                await _jewelryContext.SaveChangesAsync();
                scope.Complete();
            }

            return $"ยกเลิกการขายวัตถุดิบ {header.DocumentNo} สำเร็จ";
        }

        public async Task<string> Delete(jewelry.Model.Sale.MaterialSale.Delete.Request request)
        {
            var header = await _jewelryContext.TbtSaleMaterialHeader
                .FirstOrDefaultAsync(x => x.Running == request.Running && x.IsDelete == false);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร {request.Running}");
            }

            if (header.Status != 10)
            {
                throw new HandleException("ลบได้เฉพาะเอกสารสถานะร่าง");
            }

            header.IsDelete = true;
            header.UpdateBy = CurrentUsername;
            header.UpdateDate = DateTime.UtcNow;

            _jewelryContext.TbtSaleMaterialHeader.Update(header);
            await _jewelryContext.SaveChangesAsync();

            return $"ลบเอกสาร {header.DocumentNo} สำเร็จ";
        }
    }
}
