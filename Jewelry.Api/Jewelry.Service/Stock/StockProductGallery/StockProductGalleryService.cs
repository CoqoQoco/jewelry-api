using jewelry.Model.Exceptions;
using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Stock.StockProductGallery;

public class StockProductGalleryService : BaseService, IStockProductGalleryService
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly JewelryContext _jewelryContext;
    private readonly IAzureBlobStorageService _azureBlobService;

    public StockProductGalleryService(
        JewelryContext jewelryContext,
        IHttpContextAccessor httpContextAccessor,
        IAzureBlobStorageService azureBlobService)
        : base(jewelryContext, httpContextAccessor)
    {
        _jewelryContext = jewelryContext;
        _azureBlobService = azureBlobService;
    }

    public async Task<jewelry.Model.Stock.StockProductGallery.Get.Response> Get(jewelry.Model.Stock.StockProductGallery.Get.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.StockNumber))
        {
            throw new HandleException("ไม่พบสินค้า");
        }

        var piece = await _jewelryContext.TbtStockPiece
            .Include(p => p.SkuCodeNavigation)
            .FirstOrDefaultAsync(p => p.StockNumber == request.StockNumber.Trim());

        if (piece == null)
        {
            throw new HandleException("ไม่พบสินค้า");
        }

        var mold = piece.SkuCodeNavigation.Mold;
        var moldKey = ProductGalleryHelper.NormalizeMoldKey(mold);

        var resolved = await ProductGalleryHelper.ResolveAsync(_jewelryContext, piece.SkuCode, mold);

        var moldPieceCount = 0;
        if (moldKey != null)
        {
            moldPieceCount = await _jewelryContext.TbtStockPiece
                .Where(p => p.Status == "IN_STOCK"
                    && p.SkuCodeNavigation.Mold != null
                    && p.SkuCodeNavigation.Mold.Trim().ToUpper() == moldKey)
                .CountAsync();
        }

        return new jewelry.Model.Stock.StockProductGallery.Get.Response
        {
            StockNumber = piece.StockNumber,
            StockNumberOrigin = piece.StockNumberOrigin,
            SkuCode = piece.SkuCode,
            Mold = mold,
            MoldPieceCount = moldPieceCount,
            CanUseMoldScope = moldKey != null,
            SkuImages = resolved.SkuImages.Select(MapItem).ToList(),
            MoldImages = resolved.MoldImages.Select(MapItem).ToList(),
            Images = resolved.Images
        };
    }

    public async Task<jewelry.Model.Stock.StockProductGallery.Item> Upload(jewelry.Model.Stock.StockProductGallery.Upload.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.StockNumber))
        {
            throw new HandleException("ไม่พบสินค้า");
        }

        if (!ProductGalleryHelper.TryNormalizeScope(request.Scope, out var scopeType))
        {
            throw new HandleException("ประเภทขอบเขตรูปภาพไม่ถูกต้อง (MOLD หรือ SKU เท่านั้น)");
        }

        var image = request.Image;
        if (image == null || image.Length == 0)
        {
            throw new HandleException("กรุณาเลือกไฟล์รูปภาพ");
        }

        if (image.Length > MaxFileSizeBytes)
        {
            throw new HandleException("ไฟล์รูปภาพต้องมีขนาดไม่เกิน 5 MB");
        }

        byte[] bytes;
        using (var buffer = new MemoryStream())
        {
            await image.CopyToAsync(buffer);
            bytes = buffer.ToArray();
        }

        if (!ImageFileInspector.TryInspect(bytes, out var inspect) || inspect == null)
        {
            throw new HandleException("รองรับเฉพาะไฟล์รูปภาพ JPEG, PNG หรือ WebP เท่านั้น");
        }

        var longEdge = Math.Max(inspect.Width, inspect.Height);
        if (longEdge < 1000)
        {
            throw new HandleException("รูปความละเอียดต่ำเกินไป ด้านยาวต้องอย่างน้อย 1000px");
        }

        var (_, scopeKey) = await ResolvePieceAndScopeKeyAsync(request.StockNumber, scopeType);

        var now = DateTime.UtcNow;

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();

        var activeImages = await _jewelryContext.TbtProductGallery
            .Where(g => g.IsActive && g.ScopeType == scopeType && g.ScopeKey == scopeKey)
            .ToListAsync();

        if (activeImages.Count >= ProductGalleryHelper.MaxImagesPerScope)
        {
            throw new HandleException("รูปของแบบ/ชิ้นนี้ครบ 4 รูปแล้ว");
        }

        var nextSortOrder = activeImages.Count == 0 ? 0 : activeImages.Max(g => g.SortOrder) + 1;

        var fileName = $"{now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.{inspect.Extension}";

        using var uploadStream = new MemoryStream(bytes);
        var uploadResult = await _azureBlobService.UploadImageAsync(uploadStream, ProductGalleryHelper.BlobFolder, fileName);

        if (uploadResult == null || !uploadResult.Success)
        {
            throw new HandleException($"อัปโหลดรูปไม่สำเร็จ (โฟลเดอร์ {ProductGalleryHelper.BlobFolder} อาจไม่ได้รับอนุญาต): {uploadResult?.ErrorMessage}");
        }

        var entity = new TbtProductGallery
        {
            ScopeType = scopeType,
            ScopeKey = scopeKey,
            BlobPath = uploadResult.BlobName,
            SortOrder = nextSortOrder,
            Width = inspect.Width,
            Height = inspect.Height,
            SizeBytes = bytes.LongLength,
            ContentType = inspect.ContentType,
            IsActive = true,
            CreateDate = now,
            CreateBy = CurrentUsername ?? "system"
        };

        _jewelryContext.TbtProductGallery.Add(entity);
        await _jewelryContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return MapItem(entity);
    }

    public async Task Reorder(jewelry.Model.Stock.StockProductGallery.Reorder.Request request)
    {
        if (string.IsNullOrWhiteSpace(request.StockNumber))
        {
            throw new HandleException("ไม่พบสินค้า");
        }

        if (!ProductGalleryHelper.TryNormalizeScope(request.Scope, out var scopeType))
        {
            throw new HandleException("ประเภทขอบเขตรูปภาพไม่ถูกต้อง (MOLD หรือ SKU เท่านั้น)");
        }

        if (request.Ids == null || request.Ids.Count == 0)
        {
            throw new HandleException("กรุณาระบุลำดับรูปภาพ");
        }

        var (_, scopeKey) = await ResolvePieceAndScopeKeyAsync(request.StockNumber, scopeType);

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();

        var activeImages = await _jewelryContext.TbtProductGallery
            .Where(g => g.IsActive && g.ScopeType == scopeType && g.ScopeKey == scopeKey)
            .ToListAsync();

        var activeIds = activeImages.Select(g => g.Id).ToHashSet();
        var requestIds = request.Ids.ToHashSet();

        if (activeIds.Count != requestIds.Count || !activeIds.SetEquals(requestIds))
        {
            throw new HandleException("ลำดับรูปภาพไม่ตรงกับรูปที่มีอยู่");
        }

        var now = DateTime.UtcNow;
        var byId = activeImages.ToDictionary(g => g.Id);
        var updated = new List<TbtProductGallery>();

        for (var i = 0; i < request.Ids.Count; i++)
        {
            var img = byId[request.Ids[i]];
            img.SortOrder = i;
            img.UpdateBy = CurrentUsername ?? "system";
            img.UpdateDate = now;
            updated.Add(img);
        }

        _jewelryContext.TbtProductGallery.UpdateRange(updated);
        await _jewelryContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task Delete(jewelry.Model.Stock.StockProductGallery.Delete.Request request)
    {
        var image = await _jewelryContext.TbtProductGallery
            .FirstOrDefaultAsync(g => g.Id == request.Id && g.IsActive);

        if (image == null)
        {
            throw new HandleException("ไม่พบรูปภาพ");
        }

        var now = DateTime.UtcNow;
        var username = CurrentUsername ?? "system";

        using var transaction = await _jewelryContext.Database.BeginTransactionAsync();

        image.IsActive = false;
        image.UpdateBy = username;
        image.UpdateDate = now;
        _jewelryContext.TbtProductGallery.Update(image);
        await _jewelryContext.SaveChangesAsync();

        var remaining = await _jewelryContext.TbtProductGallery
            .Where(g => g.IsActive && g.ScopeType == image.ScopeType && g.ScopeKey == image.ScopeKey)
            .OrderBy(g => g.SortOrder)
            .ToListAsync();

        for (var i = 0; i < remaining.Count; i++)
        {
            remaining[i].SortOrder = i;
            remaining[i].UpdateBy = username;
            remaining[i].UpdateDate = now;
        }

        _jewelryContext.TbtProductGallery.UpdateRange(remaining);
        await _jewelryContext.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<(TbtStockPiece Piece, string ScopeKey)> ResolvePieceAndScopeKeyAsync(string stockNumber, string scopeType)
    {
        var piece = await _jewelryContext.TbtStockPiece
            .Include(p => p.SkuCodeNavigation)
            .FirstOrDefaultAsync(p => p.StockNumber == stockNumber.Trim());

        if (piece == null)
        {
            throw new HandleException("ไม่พบสินค้า");
        }

        if (scopeType == ProductGalleryHelper.ScopeMold)
        {
            var moldKey = ProductGalleryHelper.NormalizeMoldKey(piece.SkuCodeNavigation.Mold);
            if (moldKey == null)
            {
                throw new HandleException("สินค้านี้ไม่มีแบบ (mold) ไม่สามารถใช้รูปตามแบบได้");
            }

            return (piece, moldKey);
        }

        return (piece, piece.SkuCode);
    }

    private static jewelry.Model.Stock.StockProductGallery.Item MapItem(TbtProductGallery g) => new jewelry.Model.Stock.StockProductGallery.Item
    {
        Id = g.Id,
        BlobPath = g.BlobPath,
        SortOrder = g.SortOrder,
        Width = g.Width,
        Height = g.Height,
        CreateDate = g.CreateDate,
        CreateBy = g.CreateBy
    };
}
