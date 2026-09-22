using Jewelry.Data.Context;
using Jewelry.Data.Models.Jewelry;
using Microsoft.EntityFrameworkCore;

namespace Jewelry.Service.Helper;

public class GalleryResolveResult
{
    public string? MoldKey { get; set; }
    public List<TbtProductGallery> SkuImages { get; set; } = new();
    public List<TbtProductGallery> MoldImages { get; set; } = new();
    public List<string> Images { get; set; } = new();
}

public static class ProductGalleryHelper
{
    public const string ScopeMold = "MOLD";
    public const string ScopeSku = "SKU";
    public const string BlobFolder = "Stock/Gallery";
    public const int MaxImagesPerScope = 4;
    public const int MaxDisplayImages = 4;

    public static string? NormalizeMoldKey(string? mold)
        => string.IsNullOrWhiteSpace(mold) ? null : mold.Trim().ToUpperInvariant();

    public static bool TryNormalizeScope(string? scope, out string scopeType)
    {
        scopeType = (scope ?? string.Empty).Trim().ToUpperInvariant();
        return scopeType == ScopeSku || scopeType == ScopeMold;
    }

    public static IQueryable<TbtProductGallery> QueryActive(JewelryContext context, string scopeType, string scopeKey)
        => context.TbtProductGallery
            .AsNoTracking()
            .Where(g => g.IsActive && g.ScopeType == scopeType && g.ScopeKey == scopeKey)
            .OrderBy(g => g.SortOrder);

    public static List<string> CombineDisplayImages(IEnumerable<TbtProductGallery> skuImagesOrdered, IEnumerable<TbtProductGallery> moldImagesOrdered)
        => skuImagesOrdered.Select(x => x.BlobPath)
            .Concat(moldImagesOrdered.Select(x => x.BlobPath))
            .Take(MaxDisplayImages)
            .ToList();

    // ใช้ใน service ที่เป็น async (เช่น StockProductGalleryService.Get)
    public static async Task<GalleryResolveResult> ResolveAsync(JewelryContext context, string skuCode, string? mold)
    {
        var moldKey = NormalizeMoldKey(mold);

        var skuImages = await QueryActive(context, ScopeSku, skuCode).ToListAsync();
        var moldImages = moldKey == null
            ? new List<TbtProductGallery>()
            : await QueryActive(context, ScopeMold, moldKey).ToListAsync();

        return new GalleryResolveResult
        {
            MoldKey = moldKey,
            SkuImages = skuImages,
            MoldImages = moldImages,
            Images = CombineDisplayImages(skuImages, moldImages)
        };
    }

    // ใช้ใน service ที่เป็น sync (เช่น PublicProductService.BuildResponse)
    public static GalleryResolveResult Resolve(JewelryContext context, string skuCode, string? mold)
    {
        var moldKey = NormalizeMoldKey(mold);

        var skuImages = QueryActive(context, ScopeSku, skuCode).ToList();
        var moldImages = moldKey == null
            ? new List<TbtProductGallery>()
            : QueryActive(context, ScopeMold, moldKey).ToList();

        return new GalleryResolveResult
        {
            MoldKey = moldKey,
            SkuImages = skuImages,
            MoldImages = moldImages,
            Images = CombineDisplayImages(skuImages, moldImages)
        };
    }

    // input อาจเป็น stock_number (เว้น/ไม่เว้นขีด) หรือ stock_number_origin (เลขที่ผลิตเก่า)
    // ถ้าเจอหลายชิ้น เลือกที่ status = IN_STOCK ก่อน แล้วเอาชิ้นล่าสุด
    public static async Task<TbtStockPiece?> ResolvePieceAsync(JewelryContext context, string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var trimmed = input.Trim();

        var candidates = await context.TbtStockPiece
            .Include(p => p.SkuCodeNavigation)
            .Where(p => p.StockNumber == trimmed)
            .ToListAsync();

        if (candidates.Count == 0)
        {
            var normalized = StockNumberSearch.Normalize(trimmed);
            if (!string.IsNullOrEmpty(normalized))
            {
                candidates = await context.TbtStockPiece
                    .Include(p => p.SkuCodeNavigation)
                    .Where(p => p.StockNumber.Replace("-", "") == normalized)
                    .ToListAsync();
            }
        }

        if (candidates.Count == 0)
        {
            var upperTrimmed = trimmed.ToUpper();
            candidates = await context.TbtStockPiece
                .Include(p => p.SkuCodeNavigation)
                .Where(p => p.StockNumberOrigin != null && p.StockNumberOrigin.Trim().ToUpper() == upperTrimmed)
                .ToListAsync();
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        return candidates
            .OrderByDescending(p => p.Status == "IN_STOCK")
            .ThenByDescending(p => p.CreateDate)
            .First();
    }
}
