using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Jewelry.Service.Sale.ExportShipment;

public class ExportShipmentService : BaseService, IExportShipmentService
{
    private readonly JewelryContext _jewelryContext;
    private readonly IRunningNumber _runningNumberService;

    public ExportShipmentService(JewelryContext jewelryContext, IHttpContextAccessor httpContextAccessor,
        IRunningNumber runningNumberService)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _runningNumberService = runningNumberService;
    }

    public async Task<jewelry.Model.Sale.ExportShipment.GenerateNumber.Response> GenerateNumber()
    {
        var documentNumber = await _runningNumberService.GenerateRunningNumberForGold("EXP");
        return new jewelry.Model.Sale.ExportShipment.GenerateNumber.Response { DocumentNumber = documentNumber };
    }

    public async Task<jewelry.Model.Sale.ExportShipment.Upsert.Response> Upsert(jewelry.Model.Sale.ExportShipment.Upsert.Request request)
    {
        TbtExportShipment header;

        if (string.IsNullOrEmpty(request.Running))
        {
            var documentNumber = await _runningNumberService.GenerateRunningNumberForGold("EXP");

            header = new TbtExportShipment
            {
                Running = Guid.NewGuid().ToString(),
                DocumentNumber = documentNumber,
                Status = 0,
                StatusName = "Draft",
                IsActive = true,
                CreateDate = DateTime.UtcNow,
                CreateBy = CurrentUsername
            };

            await _jewelryContext.TbtExportShipment.AddAsync(header);
        }
        else
        {
            header = await _jewelryContext.TbtExportShipment
                .FirstOrDefaultAsync(x => x.Running == request.Running);

            if (header == null)
            {
                throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
            }

            header.UpdateDate = DateTime.UtcNow;
            header.UpdateBy = CurrentUsername;

            _jewelryContext.TbtExportShipment.Update(header);
        }

        header.CustomNumber = request.CustomNumber;
        header.DocumentDate = request.DocumentDate?.UtcDateTime ?? DateTime.UtcNow;
        header.ConsigneeName = request.ConsigneeName;
        header.ConsigneeAddress = request.ConsigneeAddress;
        header.EventName = request.EventName;
        header.BoothNo = request.BoothNo;
        header.AttnName = request.AttnName;
        header.AttnPassport = request.AttnPassport;
        header.AttnTel = request.AttnTel;
        header.Incoterm = request.Incoterm;
        header.OriginCountry = request.OriginCountry;
        header.Currency = request.Currency;
        header.ExchangeRate = request.ExchangeRate;
        header.PricePercent = request.PricePercent;
        header.ParcelCount = request.ParcelCount;
        header.Remark = request.Remark;

        await _jewelryContext.SaveChangesAsync();

        await SyncItems(header, request.Items ?? new List<jewelry.Model.Sale.ExportShipment.Upsert.ItemRequest>());

        return new jewelry.Model.Sale.ExportShipment.Upsert.Response
        {
            Running = header.Running,
            DocumentNumber = header.DocumentNumber
        };
    }

    public async Task<jewelry.Model.Sale.ExportShipment.Get.Response> Get(string running)
    {
        var header = await _jewelryContext.TbtExportShipment
            .FirstOrDefaultAsync(x => x.Running == running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {running}");
        }

        return new jewelry.Model.Sale.ExportShipment.Get.Response
        {
            Running = header.Running,
            DocumentNumber = header.DocumentNumber,
            CustomNumber = header.CustomNumber,
            DocumentDate = header.DocumentDate,
            ConsigneeName = header.ConsigneeName,
            ConsigneeAddress = header.ConsigneeAddress,
            EventName = header.EventName,
            BoothNo = header.BoothNo,
            AttnName = header.AttnName,
            AttnPassport = header.AttnPassport,
            AttnTel = header.AttnTel,
            Incoterm = header.Incoterm,
            OriginCountry = header.OriginCountry,
            Currency = header.Currency,
            ExchangeRate = header.ExchangeRate,
            PricePercent = header.PricePercent,
            ParcelCount = header.ParcelCount,
            Remark = header.Remark,
            Status = header.Status,
            StatusName = header.StatusName,
            CreateDate = header.CreateDate,
            CreateBy = header.CreateBy,
            Items = await GetItemDtos(running)
        };
    }

    public IQueryable<jewelry.Model.Sale.ExportShipment.List.Response> List(jewelry.Model.Sale.ExportShipment.List.Search? search)
    {
        var query = _jewelryContext.TbtExportShipment
            .Where(x => x.IsActive)
            .AsQueryable();

        if (search != null)
        {
            if (!string.IsNullOrEmpty(search.Keyword))
            {
                var keyword = search.Keyword;
                query = query.Where(x =>
                    x.DocumentNumber.Contains(keyword) ||
                    (x.CustomNumber != null && x.CustomNumber.Contains(keyword)) ||
                    (x.EventName != null && x.EventName.Contains(keyword)) ||
                    (x.ConsigneeName != null && x.ConsigneeName.Contains(keyword)));
            }

            if (search.DateFrom.HasValue)
            {
                var startUtc = search.DateFrom.Value.StartOfDayUtc().UtcDateTime;
                query = query.Where(x => x.DocumentDate >= startUtc);
            }

            if (search.DateTo.HasValue)
            {
                var endUtc = search.DateTo.Value.EndOfDayUtc().UtcDateTime;
                query = query.Where(x => x.DocumentDate <= endUtc);
            }

            if (search.Status.HasValue)
            {
                query = query.Where(x => x.Status == search.Status.Value);
            }
        }

        return query
            .OrderByDescending(x => x.CreateDate)
            .Select(x => new jewelry.Model.Sale.ExportShipment.List.Response
            {
                Running = x.Running,
                DocumentNumber = x.DocumentNumber,
                CustomNumber = x.CustomNumber,
                DocumentDate = x.DocumentDate,
                ConsigneeName = x.ConsigneeName,
                EventName = x.EventName,
                BoothNo = x.BoothNo,
                Currency = x.Currency,
                ParcelCount = x.ParcelCount,
                ItemCount = x.TbtExportShipmentItem.Count,
                Status = x.Status,
                StatusName = x.StatusName,
                CreateDate = x.CreateDate,
                CreateBy = x.CreateBy
            });
    }

    public async Task Delete(string running)
    {
        var header = await _jewelryContext.TbtExportShipment
            .FirstOrDefaultAsync(x => x.Running == running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {running}");
        }

        header.IsActive = false;
        header.UpdateDate = DateTime.UtcNow;
        header.UpdateBy = CurrentUsername;

        _jewelryContext.TbtExportShipment.Update(header);
        await _jewelryContext.SaveChangesAsync();
    }

    public async Task<jewelry.Model.Sale.ExportShipment.AddItems.Response> AddItems(jewelry.Model.Sale.ExportShipment.AddItems.Request request)
    {
        var header = await _jewelryContext.TbtExportShipment
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
        }

        var existingStockNumbers = new HashSet<string>(
            await _jewelryContext.TbtExportShipmentItem
                .Where(x => x.ShipmentRunning == request.Running)
                .Select(x => x.StockNumber)
                .ToListAsync(),
            StringComparer.OrdinalIgnoreCase);

        List<TbtStockPiece> candidatePieces;

        if (request.StockNumbers != null && request.StockNumbers.Any())
        {
            var stockNumbersSet = request.StockNumbers.Distinct().ToList();

            candidatePieces = await _jewelryContext.TbtStockPiece
                .Include(p => p.SkuCodeNavigation)
                .Where(p => stockNumbersSet.Contains(p.StockNumber) && p.Status == "IN_STOCK")
                .ToListAsync();
        }
        else
        {
            var pieceQuery = _jewelryContext.TbtStockPiece
                .Include(p => p.SkuCodeNavigation)
                .Where(p => p.Status == "IN_STOCK")
                .AsQueryable();

            var filter = request.Filter;
            if (filter != null)
            {
                if (filter.LocationCodes != null && filter.LocationCodes.Any())
                {
                    pieceQuery = pieceQuery.Where(p => filter.LocationCodes.Contains(p.LocationCode));
                }

                if (filter.ProductType != null && filter.ProductType.Any())
                {
                    pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductType != null && filter.ProductType.Contains(p.SkuCodeNavigation.ProductType));
                }

                if (filter.ProductionType != null && filter.ProductionType.Any())
                {
                    pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductionType != null && filter.ProductionType.Contains(p.SkuCodeNavigation.ProductionType));
                }

                if (filter.ProductionTypeSize != null && filter.ProductionTypeSize.Any())
                {
                    pieceQuery = pieceQuery.Where(p => p.SkuCodeNavigation.ProductionTypeSize != null && filter.ProductionTypeSize.Contains(p.SkuCodeNavigation.ProductionTypeSize));
                }

                if (!string.IsNullOrEmpty(filter.ReceiptNumber))
                {
                    pieceQuery = pieceQuery.Where(p => p.ReceiptNumber != null && p.ReceiptNumber.Contains(filter.ReceiptNumber));
                }

                if (!string.IsNullOrEmpty(filter.Keyword))
                {
                    var keyword = filter.Keyword;
                    pieceQuery = pieceQuery.Where(p =>
                        p.StockNumber.Contains(keyword) ||
                        p.ProductCode.Contains(keyword) ||
                        (p.SkuCodeNavigation.ProductNumber != null && p.SkuCodeNavigation.ProductNumber.Contains(keyword)) ||
                        p.SkuCodeNavigation.ProductNameEn.Contains(keyword) ||
                        p.SkuCodeNavigation.ProductNameTh.Contains(keyword));
                }
            }

            candidatePieces = await pieceQuery.ToListAsync();
        }

        var toAddPieces = new List<TbtStockPiece>();
        var seenInThisCall = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skipped = 0;

        foreach (var piece in candidatePieces)
        {
            if (existingStockNumbers.Contains(piece.StockNumber) || !seenInThisCall.Add(piece.StockNumber))
            {
                skipped++;
                continue;
            }

            toAddPieces.Add(piece);
        }

        if (toAddPieces.Any())
        {
            var stockNumbersToFetch = toAddPieces.Select(p => p.StockNumber).ToList();

            var materialsByStock = (await _jewelryContext.TbtStockPieceMaterial
                    .Where(m => stockNumbersToFetch.Contains(m.StockNumber))
                    .ToListAsync())
                .GroupBy(m => m.StockNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            var maxSortOrder = await _jewelryContext.TbtExportShipmentItem
                .Where(x => x.ShipmentRunning == request.Running)
                .Select(x => (int?)x.SortOrder)
                .MaxAsync() ?? 0;

            var newEntities = new List<TbtExportShipmentItem>();
            var sortOrder = maxSortOrder;

            foreach (var piece in toAddPieces)
            {
                sortOrder++;
                materialsByStock.TryGetValue(piece.StockNumber, out var materials);
                newEntities.Add(BuildItem(header, piece, piece.SkuCodeNavigation, materials ?? new List<TbtStockPieceMaterial>(), sortOrder));
            }

            await _jewelryContext.TbtExportShipmentItem.AddRangeAsync(newEntities);
            await _jewelryContext.SaveChangesAsync();

            await RenumberItems(request.Running);
        }

        return new jewelry.Model.Sale.ExportShipment.AddItems.Response
        {
            Added = toAddPieces.Count,
            Skipped = skipped,
            Items = await GetItemDtos(request.Running)
        };
    }

    public async Task<jewelry.Model.Sale.ExportShipment.RemoveItems.Response> RemoveItems(jewelry.Model.Sale.ExportShipment.RemoveItems.Request request)
    {
        var header = await _jewelryContext.TbtExportShipment
            .FirstOrDefaultAsync(x => x.Running == request.Running);

        if (header == null)
        {
            throw new HandleException($"ไม่พบเอกสาร Running: {request.Running}");
        }

        if (request.ItemIds != null && request.ItemIds.Any())
        {
            var items = await _jewelryContext.TbtExportShipmentItem
                .Where(x => x.ShipmentRunning == request.Running && request.ItemIds.Contains(x.Id))
                .ToListAsync();

            if (items.Any())
            {
                _jewelryContext.TbtExportShipmentItem.RemoveRange(items);
                await _jewelryContext.SaveChangesAsync();
            }
        }

        await RenumberItems(request.Running);

        return new jewelry.Model.Sale.ExportShipment.RemoveItems.Response
        {
            Items = await GetItemDtos(request.Running)
        };
    }

    private async Task SyncItems(TbtExportShipment header, List<jewelry.Model.Sale.ExportShipment.Upsert.ItemRequest> requestItems)
    {
        var existingItems = await _jewelryContext.TbtExportShipmentItem
            .Where(x => x.ShipmentRunning == header.Running)
            .ToListAsync();

        var existingById = existingItems.ToDictionary(x => x.Id);
        var payloadIds = new HashSet<long>(requestItems.Where(x => x.Id.HasValue).Select(x => x.Id!.Value));

        var toDelete = existingItems.Where(x => !payloadIds.Contains(x.Id)).ToList();
        if (toDelete.Any())
        {
            _jewelryContext.TbtExportShipmentItem.RemoveRange(toDelete);
        }

        var toUpdate = new List<TbtExportShipmentItem>();
        foreach (var itemReq in requestItems.Where(x => x.Id.HasValue))
        {
            if (!existingById.TryGetValue(itemReq.Id!.Value, out var entity))
            {
                continue;
            }

            entity.Description = itemReq.Description;
            entity.Qty = itemReq.Qty;
            entity.UnitPrice = itemReq.UnitPrice;
            entity.Amount = (itemReq.UnitPrice ?? 0) * itemReq.Qty;
            entity.ParcelNo = itemReq.ParcelNo;
            entity.SortOrder = itemReq.SortOrder;
            entity.UpdateDate = DateTime.UtcNow;
            entity.UpdateBy = CurrentUsername;

            toUpdate.Add(entity);
        }

        if (toUpdate.Any())
        {
            _jewelryContext.TbtExportShipmentItem.UpdateRange(toUpdate);
        }

        var survivingStockNumbers = new HashSet<string>(
            existingItems.Where(x => payloadIds.Contains(x.Id)).Select(x => x.StockNumber),
            StringComparer.OrdinalIgnoreCase);

        var toInsertRequests = requestItems
            .Where(x => !x.Id.HasValue && !string.IsNullOrEmpty(x.StockNumber) && !survivingStockNumbers.Contains(x.StockNumber))
            .ToList();

        if (toInsertRequests.Any())
        {
            var stockNumbers = toInsertRequests.Select(x => x.StockNumber).Distinct().ToList();

            var pieces = await _jewelryContext.TbtStockPiece
                .Include(p => p.SkuCodeNavigation)
                .Where(p => stockNumbers.Contains(p.StockNumber))
                .ToListAsync();

            var materialsByStock = (await _jewelryContext.TbtStockPieceMaterial
                    .Where(m => stockNumbers.Contains(m.StockNumber))
                    .ToListAsync())
                .GroupBy(m => m.StockNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            var newEntities = new List<TbtExportShipmentItem>();

            foreach (var itemReq in toInsertRequests)
            {
                var piece = pieces.FirstOrDefault(p => p.StockNumber == itemReq.StockNumber);
                if (piece == null)
                {
                    continue;
                }

                materialsByStock.TryGetValue(itemReq.StockNumber, out var materials);
                var entity = BuildItem(header, piece, piece.SkuCodeNavigation, materials ?? new List<TbtStockPieceMaterial>(), itemReq.SortOrder);

                entity.Description = itemReq.Description ?? entity.Description;
                entity.Qty = itemReq.Qty;
                entity.UnitPrice = itemReq.UnitPrice ?? entity.UnitPrice;
                entity.Amount = (entity.UnitPrice ?? 0) * itemReq.Qty;
                entity.ParcelNo = itemReq.ParcelNo;

                newEntities.Add(entity);
            }

            if (newEntities.Any())
            {
                await _jewelryContext.TbtExportShipmentItem.AddRangeAsync(newEntities);
            }
        }

        await _jewelryContext.SaveChangesAsync();
        await RenumberItems(header.Running);
    }

    private TbtExportShipmentItem BuildItem(TbtExportShipment header, TbtStockPiece piece, TbtSku? sku, List<TbtStockPieceMaterial> materials, int sortOrder)
    {
        var snapshot = ComputeMaterialSnapshot(materials, sku);

        var tagPrice = sku?.DefaultPrice;
        decimal unitPrice = 0;

        if (header.ExchangeRate.HasValue && header.ExchangeRate.Value != 0)
        {
            var pricePercent = header.PricePercent ?? 100m;
            unitPrice = Math.Round((tagPrice ?? 0m) * (pricePercent / 100m) / header.ExchangeRate.Value, 2);
        }

        const decimal qty = 1m;

        return new TbtExportShipmentItem
        {
            ShipmentRunning = header.Running,
            ItemNo = 0,
            SortOrder = sortOrder,
            StockNumber = piece.StockNumber,
            ProductCode = piece.ProductCode,
            ProductNumber = sku?.ProductNumber,
            Description = snapshot.Description,
            GoldWeight = snapshot.GoldWeight,
            StoneWeight = snapshot.StoneWeight,
            DiamondWeight = snapshot.DiamondWeight,
            NetWeight = snapshot.NetWeight,
            Qty = qty,
            TagPrice = tagPrice,
            UnitPrice = unitPrice,
            Amount = unitPrice * qty,
            ImagePath = sku?.ImagePath,
            ParcelNo = 1,
            CreateBy = CurrentUsername,
            CreateDate = DateTime.UtcNow
        };
    }

    private static (decimal GoldWeight, decimal StoneWeight, decimal DiamondWeight, decimal NetWeight, string Description) ComputeMaterialSnapshot(
        List<TbtStockPieceMaterial> materials, TbtSku? sku)
    {
        decimal goldWeight = 0, stoneWeight = 0, diamondWeight = 0;
        var stoneNames = new List<(string Name, bool IsDiamond)>();

        foreach (var m in materials)
        {
            var weight = m.Weight ?? 0;

            if (string.Equals(m.Type, "Gold", StringComparison.OrdinalIgnoreCase))
            {
                goldWeight += weight;
                continue;
            }

            var isGem = string.Equals(m.Type, "Gem", StringComparison.OrdinalIgnoreCase);
            var isDiamondType = string.Equals(m.Type, "Diamond", StringComparison.OrdinalIgnoreCase);

            if (!isGem && !isDiamondType)
            {
                continue;
            }

            var isDiamond = isDiamondType ||
                (m.TypeOrigin != null && m.TypeOrigin.Contains("diamond", StringComparison.OrdinalIgnoreCase)) ||
                (m.TypeName != null && m.TypeName.Contains("diamond", StringComparison.OrdinalIgnoreCase));

            if (isDiamond)
            {
                diamondWeight += weight;
            }
            else
            {
                stoneWeight += weight;
            }

            var rawName = m.TypeOrigin ?? m.TypeName;
            if (!string.IsNullOrWhiteSpace(rawName))
            {
                var cleanName = Regex.Replace(rawName, @"\(.*?\)", "").Trim();
                if (!string.IsNullOrEmpty(cleanName))
                {
                    stoneNames.Add((cleanName, isDiamond));
                }
            }
        }

        var netWeight = Math.Round(goldWeight + (stoneWeight + diamondWeight) / 5m, 3);

        var distinctNames = stoneNames
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Name = g.Key, IsDiamond = g.Any(x => x.IsDiamond) })
            .OrderByDescending(x => x.IsDiamond)
            .Select(x => x.Name)
            .ToList();

        var productTypeEn = MapProductTypeEn(sku?.ProductType);
        string description;

        if (distinctNames.Any())
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(sku?.ProductionTypeSize)) parts.Add(sku.ProductionTypeSize);
            if (!string.IsNullOrWhiteSpace(productTypeEn)) parts.Add(productTypeEn);
            parts.Add(string.Join("/", distinctNames));
            description = string.Join(" ", parts);
        }
        else
        {
            description = sku?.ProductNameEn ?? string.Empty;
        }

        return (goldWeight, stoneWeight, diamondWeight, netWeight, description);
    }

    private static string MapProductTypeEn(string? productType)
    {
        if (string.IsNullOrWhiteSpace(productType))
        {
            return productType ?? string.Empty;
        }

        return productType.Trim().ToUpperInvariant() switch
        {
            "R" => "RING",
            "P" => "PENDANT",
            "ES" or "E" or "EL" or "EH" => "EARRING",
            "B" => "BRACELET",
            "N" => "NECKLACE",
            "G" => "BANGLE",
            "CH" => "CHAIN",
            _ => productType
        };
    }

    private async Task RenumberItems(string running)
    {
        var items = await _jewelryContext.TbtExportShipmentItem
            .Where(x => x.ShipmentRunning == running)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync();

        for (var i = 0; i < items.Count; i++)
        {
            items[i].ItemNo = i + 1;
        }

        _jewelryContext.TbtExportShipmentItem.UpdateRange(items);
        await _jewelryContext.SaveChangesAsync();
    }

    private async Task<List<jewelry.Model.Sale.ExportShipment.Common.ItemDto>> GetItemDtos(string running)
    {
        return await _jewelryContext.TbtExportShipmentItem
            .Where(x => x.ShipmentRunning == running)
            .OrderBy(x => x.SortOrder)
            .Select(x => new jewelry.Model.Sale.ExportShipment.Common.ItemDto
            {
                Id = x.Id,
                ItemNo = x.ItemNo,
                SortOrder = x.SortOrder,
                StockNumber = x.StockNumber,
                ProductCode = x.ProductCode,
                ProductNumber = x.ProductNumber,
                Description = x.Description,
                GoldWeight = x.GoldWeight,
                StoneWeight = x.StoneWeight,
                DiamondWeight = x.DiamondWeight,
                NetWeight = x.NetWeight,
                Qty = x.Qty,
                TagPrice = x.TagPrice,
                UnitPrice = x.UnitPrice,
                Amount = x.Amount,
                ImagePath = x.ImagePath,
                ParcelNo = x.ParcelNo
            })
            .ToListAsync();
    }
}
