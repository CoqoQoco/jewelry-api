using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Jewelry.Service.Stock;
using Kendo.DynamicLinqCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Stock.StockConvert;

public class StockConvertService : BaseService, IStockConvertService
{
    private readonly JewelryContext _jewelryContext;
    private readonly IRunningNumber _runningNumberService;

    public StockConvertService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
        IRunningNumber runningNumberService)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _runningNumberService = runningNumberService;
    }

    public async Task<jewelry.Model.Stock.StockConvert.Create.Response> Create(jewelry.Model.Stock.StockConvert.Create.Request request)
    {
        var sourceStockNumbers = (request.SourceStockNumbers ?? new List<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        if (!sourceStockNumbers.Any())
        {
            throw new HandleException("ต้องระบุสินค้าต้นทางอย่างน้อย 1 ชิ้น");
        }

        var running = await _runningNumberService.GenerateRunningNumberForGold("CV");
        var now = DateTime.UtcNow;

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
        try
        {
            // Lock order: stock piece -> stock balance (ตาม StockLockHelper)
            await _jewelryContext.LockStockPiecesAsync(sourceStockNumbers);

            var pieces = await _jewelryContext.TbtStockPiece
                .Where(p => sourceStockNumbers.Contains(p.StockNumber))
                .ToListAsync();

            var skuCodes = pieces.Select(p => p.SkuCode).Distinct().ToList();
            await _jewelryContext.LockStockBalancesAsync(skuCodes);

            var foundStockNumbers = pieces.Select(p => p.StockNumber).ToHashSet();
            var missing = sourceStockNumbers.Where(x => !foundStockNumbers.Contains(x)).ToList();
            if (missing.Any())
            {
                throw new HandleException($"ไม่พบสินค้าต้นทาง: {string.Join(", ", missing)}");
            }

            foreach (var piece in pieces)
            {
                if (piece.Status != "IN_STOCK" || StockPieceQtyHelper.Available(piece) < 1)
                {
                    throw new HandleException($"สินค้า {piece.StockNumber} ไม่พร้อมสำหรับแปลง (สถานะ {piece.Status})");
                }
            }

            var header = new TbtStockConvertHeader
            {
                Running = running,
                SoNumber = request.SoNumber,
                SoLineKey = request.SoLineKey,
                Status = 0,
                StatusName = "กำลังแปลง",
                ConvertCost = 0,
                Remark = request.Remark,
                CreateDate = now,
                CreateBy = CurrentUsername
            };
            _jewelryContext.TbtStockConvertHeader.Add(header);

            foreach (var piece in pieces)
            {
                piece.QtyReserved += 1;
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateDate = now;
                piece.UpdateBy = CurrentUsername;
                _jewelryContext.TbtStockPiece.Update(piece);

                var balance = await _jewelryContext.TbtStockBalance
                    .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);
                if (balance != null)
                {
                    balance.QtyReserved += 1;
                    balance.LastMovementAt = now;
                    _jewelryContext.TbtStockBalance.Update(balance);
                }

                _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                {
                    MovementDate = now,
                    MovementType = "RESERVE",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = piece.LocationCode,
                    Qty = 1,
                    RefDocType = "CONVERT",
                    RefDocNo = running,
                    CreateDate = now,
                    CreateBy = CurrentUsername
                });

                _jewelryContext.TbtStockConvertItem.Add(new TbtStockConvertItem
                {
                    HeaderRunning = running,
                    Role = "SOURCE",
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    SkuCode = piece.SkuCode,
                    LocationCode = piece.LocationCode,
                    Qty = 1,
                    ProductCost = piece.ProductCost,
                    CreateDate = now,
                    CreateBy = CurrentUsername
                });
            }

            await _jewelryContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new jewelry.Model.Stock.StockConvert.Create.Response { Running = running };
        }
        catch (HandleException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new HandleException($"เกิดข้อผิดพลาดในการสร้างใบแปลงสินค้า: {ex.Message}");
        }
    }

    public async Task<jewelry.Model.Stock.StockConvert.Complete.Response> Complete(jewelry.Model.Stock.StockConvert.Complete.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Running))
        {
            throw new HandleException("Running is required");
        }
        if (request.Result == null || string.IsNullOrWhiteSpace(request.Result.ProductNumber))
        {
            throw new HandleException("Result.ProductNumber is required");
        }

        var header = await _jewelryContext.TbtStockConvertHeader
            .Include(x => x.TbtStockConvertItem)
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
        }
        if (header.Status != 0)
        {
            throw new HandleException($"เอกสาร {header.Running} ไม่ได้อยู่ในสถานะกำลังแปลง ไม่สามารถปิดเอกสารได้");
        }

        var sourceItems = header.TbtStockConvertItem.Where(i => i.Role == "SOURCE").ToList();
        if (!sourceItems.Any())
        {
            throw new HandleException("ไม่พบรายการสินค้าต้นทางของเอกสารนี้");
        }

        var sourceStockNumbers = sourceItems.Select(i => i.StockNumber).ToList();

        // สร้างเลขสต็อกผลลัพธ์ก่อนเปิด transaction เพราะ Next() ทำ SaveChanges บน context ที่ใช้ร่วมกัน
        var productType = !string.IsNullOrWhiteSpace(request.Result.ProductType)
            ? await _jewelryContext.TbmProductType.FirstOrDefaultAsync(x => x.Code == request.Result.ProductType)
            : null;
        var stockPrefix = productType?.ProductCode ?? "DK";
        var resultStockNumber = await _runningNumberService.GenerateRunningNumberForStockProductHash(stockPrefix);

        var now = DateTime.UtcNow;

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
        try
        {
            await _jewelryContext.LockStockPiecesAsync(sourceStockNumbers);

            var pieces = await _jewelryContext.TbtStockPiece
                .Where(p => sourceStockNumbers.Contains(p.StockNumber))
                .ToListAsync();

            var skuCodes = pieces.Select(p => p.SkuCode).Distinct().ToList();
            await _jewelryContext.LockStockBalancesAsync(skuCodes);

            var foundStockNumbers = pieces.Select(p => p.StockNumber).ToHashSet();
            var missing = sourceStockNumbers.Where(x => !foundStockNumbers.Contains(x)).ToList();
            if (missing.Any())
            {
                throw new HandleException($"ไม่พบสินค้าต้นทาง: {string.Join(", ", missing)}");
            }

            decimal sourceCostSum = 0;

            foreach (var piece in pieces)
            {
                piece.Qty -= 1;
                piece.QtyReserved -= 1;
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateDate = now;
                piece.UpdateBy = CurrentUsername;
                _jewelryContext.TbtStockPiece.Update(piece);

                var balance = await _jewelryContext.TbtStockBalance
                    .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);
                if (balance != null)
                {
                    balance.QtyOnHand -= 1;
                    balance.QtyReserved -= 1;
                    balance.LastMovementAt = now;
                    _jewelryContext.TbtStockBalance.Update(balance);
                }

                _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                {
                    MovementDate = now,
                    MovementType = "TRANSFORM_OUT",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    FromLocation = piece.LocationCode,
                    Qty = 1,
                    RefDocType = "CONVERT",
                    RefDocNo = header.Running,
                    CreateDate = now,
                    CreateBy = CurrentUsername
                });

                sourceCostSum += piece.ProductCost ?? 0;
            }

            var locationCode = !string.IsNullOrWhiteSpace(request.LocationCode)
                ? request.LocationCode!
                : pieces.First().LocationCode;

            var skuCode = $"SKU-{request.Result.ProductNumber.ToUpper()}";
            var productNumberUpper = request.Result.ProductNumber.ToUpper();

            var sku = await _jewelryContext.TbtSku.FirstOrDefaultAsync(x => x.SkuCode == skuCode);
            if (sku == null)
            {
                sku = new TbtSku
                {
                    SkuCode = skuCode,
                    ProductNumber = productNumberUpper,
                    ProductNameTh = request.Result.ProductNameTh ?? "",
                    ProductNameEn = request.Result.ProductNameEn ?? "",
                    ProductType = request.Result.ProductType,
                    ProductTypeName = productType?.NameTh,
                    Mold = request.Result.Mold,
                    MoldDesign = request.Result.MoldDesign,
                    Size = request.Result.Size,
                    ProductionType = request.Result.ProductionType,
                    ProductionTypeSize = request.Result.ProductionTypeSize,
                    ImagePath = request.Result.ImagePath,
                    DefaultPrice = request.Result.DefaultPrice,
                    IsActive = true,
                    IsSerialized = true,
                    CreateBy = CurrentUsername,
                    CreateDate = now
                };
                _jewelryContext.TbtSku.Add(sku);
            }

            var resultProductCost = sourceCostSum + request.ConvertCost;

            var resultPiece = new TbtStockPiece
            {
                StockNumber = resultStockNumber,
                ProductCode = productNumberUpper,
                SkuCode = skuCode,
                LocationCode = locationCode,
                Qty = 1,
                QtyReserved = 0,
                ReceiptType = "convert",
                ReceiptDate = now,
                ProductionDate = now,
                StockNumberOrigin = null,
                ProductCost = resultProductCost,
                CreateBy = CurrentUsername,
                CreateDate = now
            };
            StockPieceQtyHelper.RecalcStatus(resultPiece);
            _jewelryContext.TbtStockPiece.Add(resultPiece);

            var materials = (request.Result.Materials ?? new List<jewelry.Model.Stock.StockConvert.Complete.MaterialRequest>())
                .Select(m => new TbtStockPieceMaterial
                {
                    StockNumber = resultStockNumber,
                    ProductCode = productNumberUpper,
                    Type = m.Type,
                    TypeName = m.TypeName,
                    TypeCode = m.TypeCode,
                    TypeOrigin = m.TypeOrigin,
                    Size = m.Size,
                    Qty = m.Qty,
                    QtyUnit = m.QtyUnit,
                    Weight = m.Weight,
                    WeightUnit = m.WeightUnit,
                    Region = m.Region,
                    Price = m.Price,
                    CreateBy = CurrentUsername,
                    CreateDate = now
                }).ToList();

            if (materials.Any())
            {
                _jewelryContext.TbtStockPieceMaterial.AddRange(materials);
            }

            var resultBalance = await _jewelryContext.TbtStockBalance
                .FirstOrDefaultAsync(b => b.SkuCode == skuCode && b.LocationCode == locationCode);
            if (resultBalance != null)
            {
                resultBalance.QtyOnHand += 1;
                resultBalance.LastMovementAt = now;
                _jewelryContext.TbtStockBalance.Update(resultBalance);
            }
            else
            {
                _jewelryContext.TbtStockBalance.Add(new TbtStockBalance
                {
                    SkuCode = skuCode,
                    LocationCode = locationCode,
                    QtyOnHand = 1,
                    QtyReserved = 0,
                    LastMovementAt = now,
                    CreateBy = CurrentUsername,
                    CreateDate = now
                });
            }

            _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
            {
                MovementDate = now,
                MovementType = "TRANSFORM_IN",
                SkuCode = skuCode,
                StockNumber = resultStockNumber,
                ProductCode = productNumberUpper,
                ToLocation = locationCode,
                Qty = 1,
                RefDocType = "CONVERT",
                RefDocNo = header.Running,
                CreateDate = now,
                CreateBy = CurrentUsername
            });

            _jewelryContext.TbtStockConvertItem.Add(new TbtStockConvertItem
            {
                HeaderRunning = header.Running,
                Role = "RESULT",
                StockNumber = resultStockNumber,
                ProductCode = productNumberUpper,
                SkuCode = skuCode,
                LocationCode = locationCode,
                Qty = 1,
                ProductCost = resultProductCost,
                CreateDate = now,
                CreateBy = CurrentUsername
            });

            header.Status = 1;
            header.StatusName = "เสร็จ";
            header.ConvertCost = request.ConvertCost;
            header.CompleteDate = now;
            header.UpdateDate = now;
            header.UpdateBy = CurrentUsername;
            _jewelryContext.TbtStockConvertHeader.Update(header);

            await _jewelryContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new jewelry.Model.Stock.StockConvert.Complete.Response
            {
                Running = header.Running,
                ResultStockNumber = resultStockNumber,
                SkuCode = skuCode
            };
        }
        catch (HandleException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new HandleException($"เกิดข้อผิดพลาดในการปิดใบแปลงสินค้า: {ex.Message}");
        }
    }

    public async Task<jewelry.Model.Stock.StockConvert.Cancel.Response> Cancel(jewelry.Model.Stock.StockConvert.Cancel.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Running))
        {
            throw new HandleException("Running is required");
        }

        var header = await _jewelryContext.TbtStockConvertHeader
            .Include(x => x.TbtStockConvertItem)
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
        }
        if (header.Status != 0)
        {
            throw new HandleException($"เอกสาร {header.Running} ไม่ได้อยู่ในสถานะกำลังแปลง ไม่สามารถยกเลิกได้");
        }

        var sourceStockNumbers = header.TbtStockConvertItem
            .Where(i => i.Role == "SOURCE")
            .Select(i => i.StockNumber)
            .ToList();

        var now = DateTime.UtcNow;

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();
        try
        {
            await _jewelryContext.LockStockPiecesAsync(sourceStockNumbers);

            var pieces = await _jewelryContext.TbtStockPiece
                .Where(p => sourceStockNumbers.Contains(p.StockNumber))
                .ToListAsync();

            var skuCodes = pieces.Select(p => p.SkuCode).Distinct().ToList();
            await _jewelryContext.LockStockBalancesAsync(skuCodes);

            foreach (var piece in pieces)
            {
                piece.QtyReserved = Math.Max(0, piece.QtyReserved - 1);
                StockPieceQtyHelper.RecalcStatus(piece);
                piece.UpdateDate = now;
                piece.UpdateBy = CurrentUsername;
                _jewelryContext.TbtStockPiece.Update(piece);

                var balance = await _jewelryContext.TbtStockBalance
                    .FirstOrDefaultAsync(b => b.SkuCode == piece.SkuCode && b.LocationCode == piece.LocationCode);
                if (balance != null)
                {
                    balance.QtyReserved = Math.Max(0, balance.QtyReserved - 1);
                    balance.LastMovementAt = now;
                    _jewelryContext.TbtStockBalance.Update(balance);
                }

                _jewelryContext.TbtStockMovement.Add(new TbtStockMovement
                {
                    MovementDate = now,
                    MovementType = "UNRESERVE",
                    SkuCode = piece.SkuCode,
                    StockNumber = piece.StockNumber,
                    ProductCode = piece.ProductCode,
                    ToLocation = piece.LocationCode,
                    Qty = 1,
                    RefDocType = "CONVERT",
                    RefDocNo = header.Running,
                    CreateDate = now,
                    CreateBy = CurrentUsername
                });
            }

            header.Status = 9;
            header.StatusName = "ยกเลิก";
            header.CancelReason = request.CancelReason;
            header.UpdateDate = now;
            header.UpdateBy = CurrentUsername;
            _jewelryContext.TbtStockConvertHeader.Update(header);

            await _jewelryContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return new jewelry.Model.Stock.StockConvert.Cancel.Response
            {
                Running = header.Running,
                Status = header.Status,
                StatusName = header.StatusName
            };
        }
        catch (HandleException)
        {
            await transaction.RollbackAsync();
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new HandleException($"เกิดข้อผิดพลาดในการยกเลิกใบแปลงสินค้า: {ex.Message}");
        }
    }

    public Task<DataSourceResult> List(jewelry.Model.Stock.StockConvert.List.Request request)
    {
        var query = _jewelryContext.TbtStockConvertHeader
            .Include(x => x.TbtStockConvertItem)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.DocumentNumber))
        {
            query = query.Where(x => x.Running.Contains(request.DocumentNumber));
        }
        if (!string.IsNullOrEmpty(request.SoNumber))
        {
            query = query.Where(x => x.SoNumber != null && x.SoNumber.Contains(request.SoNumber));
        }
        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }
        if (request.DateFrom.HasValue)
        {
            query = query.Where(x => x.CreateDate >= request.DateFrom.Value.StartOfDayUtc());
        }
        if (request.DateTo.HasValue)
        {
            query = query.Where(x => x.CreateDate <= request.DateTo.Value.EndOfDayUtc());
        }
        if (!string.IsNullOrEmpty(request.SourceStockNumber))
        {
            query = query.Where(x => x.TbtStockConvertItem.Any(i => i.Role == "SOURCE" && i.StockNumber.Contains(request.SourceStockNumber)));
        }

        query = query.OrderByDescending(x => x.CreateDate);

        var dataSource = query.ToDataSourceResult(request);
        var pageEntities = dataSource.Data.Cast<TbtStockConvertHeader>().ToList();

        var result = pageEntities.Select(h => new jewelry.Model.Stock.StockConvert.List.Response
        {
            Running = h.Running,
            Status = h.Status,
            StatusName = h.StatusName,
            SoNumber = h.SoNumber,
            SourceCount = h.TbtStockConvertItem.Count(i => i.Role == "SOURCE"),
            SourceStockNumbers = string.Join(", ", h.TbtStockConvertItem.Where(i => i.Role == "SOURCE").Select(i => i.StockNumber)),
            ResultStockNumber = h.TbtStockConvertItem.FirstOrDefault(i => i.Role == "RESULT")?.StockNumber,
            ConvertCost = h.ConvertCost,
            CreateDate = h.CreateDate,
            CreateBy = h.CreateBy,
            CompleteDate = h.CompleteDate
        }).ToList();

        dataSource.Data = result;
        return Task.FromResult(dataSource);
    }

    public async Task<jewelry.Model.Stock.StockConvert.Get.Response> Get(jewelry.Model.Stock.StockConvert.Get.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Running))
        {
            throw new HandleException("Running is required");
        }

        var header = await _jewelryContext.TbtStockConvertHeader
            .Include(x => x.TbtStockConvertItem)
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
        }

        var skuCodes = header.TbtStockConvertItem
            .Where(i => !string.IsNullOrEmpty(i.SkuCode))
            .Select(i => i.SkuCode!)
            .Distinct()
            .ToList();

        var skus = await _jewelryContext.TbtSku
            .Where(s => skuCodes.Contains(s.SkuCode))
            .ToDictionaryAsync(s => s.SkuCode);

        jewelry.Model.Stock.StockConvert.Get.ItemDto Map(TbtStockConvertItem item)
        {
            TbtSku? sku = null;
            if (!string.IsNullOrEmpty(item.SkuCode))
            {
                skus.TryGetValue(item.SkuCode, out sku);
            }

            return new jewelry.Model.Stock.StockConvert.Get.ItemDto
            {
                Id = item.Id,
                StockNumber = item.StockNumber,
                ProductCode = item.ProductCode,
                SkuCode = item.SkuCode,
                LocationCode = item.LocationCode,
                Qty = item.Qty,
                ProductCost = item.ProductCost,
                ProductNumber = sku?.ProductNumber,
                ProductNameEn = sku?.ProductNameEn,
                ProductNameTh = sku?.ProductNameTh
            };
        }

        return new jewelry.Model.Stock.StockConvert.Get.Response
        {
            Running = header.Running,
            SoNumber = header.SoNumber,
            SoLineKey = header.SoLineKey,
            Status = header.Status,
            StatusName = header.StatusName,
            ConvertCost = header.ConvertCost,
            Remark = header.Remark,
            CancelReason = header.CancelReason,
            CompleteDate = header.CompleteDate,
            CreateDate = header.CreateDate,
            CreateBy = header.CreateBy,
            UpdateDate = header.UpdateDate,
            UpdateBy = header.UpdateBy,
            Sources = header.TbtStockConvertItem.Where(i => i.Role == "SOURCE").Select(Map).ToList(),
            Result = header.TbtStockConvertItem.Where(i => i.Role == "RESULT").Select(Map).FirstOrDefault()
        };
    }

    public async Task<List<jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Response>> PendingForSaleOrder(jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.SoNumber))
        {
            throw new HandleException("SoNumber is required");
        }

        var headers = await _jewelryContext.TbtStockConvertHeader
            .Include(x => x.TbtStockConvertItem)
            .Where(x => x.SoNumber == request.SoNumber && x.Status == 1)
            .ToListAsync();

        var resultPairs = headers
            .Select(h => new { Header = h, Result = h.TbtStockConvertItem.FirstOrDefault(i => i.Role == "RESULT") })
            .Where(x => x.Result != null)
            .ToList();

        if (!resultPairs.Any())
        {
            return new List<jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Response>();
        }

        var resultStockNumbers = resultPairs.Select(x => x.Result!.StockNumber).ToList();

        var confirmedStockNumbers = new HashSet<string>(
            await _jewelryContext.TbtSaleOrderProduct
                .Where(p => resultStockNumbers.Contains(p.StockNumber))
                .Select(p => p.StockNumber)
                .ToListAsync());

        var pendingPairs = resultPairs.Where(x => !confirmedStockNumbers.Contains(x.Result!.StockNumber)).ToList();

        var skuCodes = pendingPairs
            .Where(x => !string.IsNullOrEmpty(x.Result!.SkuCode))
            .Select(x => x.Result!.SkuCode!)
            .Distinct()
            .ToList();
        var skus = await _jewelryContext.TbtSku
            .Where(s => skuCodes.Contains(s.SkuCode))
            .ToDictionaryAsync(s => s.SkuCode);

        var pieces = await _jewelryContext.TbtStockPiece
            .Where(p => resultStockNumbers.Contains(p.StockNumber))
            .ToDictionaryAsync(p => p.StockNumber);

        return pendingPairs.Select(x =>
        {
            TbtSku? sku = null;
            if (!string.IsNullOrEmpty(x.Result!.SkuCode))
            {
                skus.TryGetValue(x.Result!.SkuCode, out sku);
            }
            pieces.TryGetValue(x.Result!.StockNumber, out var piece);

            return new jewelry.Model.Stock.StockConvert.PendingForSaleOrder.Response
            {
                Running = x.Header.Running,
                SoLineKey = x.Header.SoLineKey,
                ResultStockNumber = x.Result!.StockNumber,
                ProductNumber = sku?.ProductNumber,
                ProductNameEn = sku?.ProductNameEn,
                QtyAvailable = piece != null ? StockPieceQtyHelper.Available(piece) : x.Result!.Qty,
                CompleteDate = x.Header.CompleteDate
            };
        }).ToList();
    }
}
