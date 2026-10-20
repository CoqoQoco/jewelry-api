using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Product.PriceCheck;
using Kendo.DynamicLinqCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jewelry.Data.Models.Jewelry;
using Jewelry.Service.Base;
using Jewelry.Service.Stock.Product;
using Microsoft.AspNetCore.Http;

namespace Jewelry.Service.Stock.PriceSanity
{
    public class PriceSanityService : BaseService, IPriceSanityService
    {
        private const int MaxItems = 500;
        private const string CacheKey = "PriceSanity:Snapshot";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
        private static readonly SemaphoreSlim BuildLock = new(1, 1);

        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PriceSanityService> _logger;

        private readonly IProductService _productService;

        public PriceSanityService(JewelryContext jewelryContext, IMemoryCache cache, ILogger<PriceSanityService> logger,
            IHttpContextAccessor httpContextAccessor, IProductService productService)
            : base(jewelryContext, httpContextAccessor)
        {
            _productService = productService;
            _jewelryContext = jewelryContext;
            _cache = cache;
            _logger = logger;
        }

        public async Task<DataSourceResult> OutlierList(jewelry.Model.Stock.Product.PriceOutlierList.Request request)
        {
            var sw = Stopwatch.StartNew();
            SnapshotBuild build;
            await BuildLock.WaitAsync();
            try
            {
                build = await BuildSnapshot();
                _cache.Set(CacheKey, build.Snap, CacheDuration);
            }
            finally
            {
                BuildLock.Release();
            }

            var activeAcks = await _jewelryContext.TbtStockPriceReviewAck.AsNoTracking()
                .Where(a => a.IsActive)
                .ToListAsync();
            var ackByStock = activeAcks
                .GroupBy(a => a.StockNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreateDate).ThenByDescending(a => a.Id).First(), StringComparer.OrdinalIgnoreCase);

            var all = new List<jewelry.Model.Stock.Product.PriceOutlierList.OutlierItem>();
            foreach (var p in build.Pieces)
            {
                if (!build.SkuByCode.TryGetValue(p.SkuCode, out var sku)) continue;
                var price = sku.DefaultPrice ?? 0m;
                if (price <= 0) continue;

                var hasMat = build.MatByKey.TryGetValue(p.StockNumber + "|" + p.ProductCode, out var mat);
                var ctx = new PriceSanityContext
                {
                    StockNumber = p.StockNumber,
                    SkuCode = p.SkuCode,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    MoldDesign = sku.MoldDesign,
                    ProductTypeName = sku.ProductTypeName,
                    ProductionType = sku.ProductionType,
                    GoldWeight = hasMat ? mat.Gold : 0m,
                    GemPrice = hasMat ? mat.Gem : 0m,
                    OldPrice = price
                };
                var reasons = PriceSanityEvaluator.Evaluate(build.Snap, ctx, price);
                if (reasons.Count == 0) continue;

                ackByStock.TryGetValue(p.StockNumber, out var ack);
                all.Add(new jewelry.Model.Stock.Product.PriceOutlierList.OutlierItem
                {
                    StockNumber = p.StockNumber,
                    StockNumberOrigin = p.StockNumberOrigin,
                    SkuCode = p.SkuCode,
                    ProductNumber = sku.ProductNumber,
                    ProductNameTh = sku.ProductNameTh,
                    ProductTypeName = sku.ProductTypeName,
                    ProductionType = sku.ProductionType,
                    Wo = p.Wo,
                    WoNumber = p.WoNumber,
                    Price = price,
                    ImagePath = sku.ImagePath,
                    Score = PriceSanityEvaluator.Score(reasons),
                    Reasons = reasons,
                    SuggestedPrice = PriceSanityEvaluator.Suggest(build.Snap, ctx, price),
                    AckPrice = ack?.PriceAcked,
                    AckBy = ack?.CreateBy,
                    AckDate = ack == null ? null : new DateTimeOffset(DateTime.SpecifyKind(ack.CreateDate, DateTimeKind.Utc)),
                    AckNote = ack?.Note,
                    IsAcked = ack != null && ack.PriceAcked == price,
                    PriceChangedSinceAck = ack != null && ack.PriceAcked != price
                });
            }

            var filter = request?.Search?.Filter;
            if (filter != null && HasAnyFilter(filter))
            {
                var allowed = (await _productService.List(filter)
                    .Select(x => x.StockNumber)
                    .Distinct()
                    .ToListAsync())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                all = all.Where(x => allowed.Contains(x.StockNumber)).ToList();
            }

            var result = PriceOutlierQuery.FilterSortPage(all, request);
            sw.Stop();
            _logger.LogInformation("PriceOutlierList: pieces={Pieces} flagged={Flagged} elapsedMs={Elapsed}",
                build.Pieces.Count, all.Count, sw.ElapsedMilliseconds);
            return result;
        }

        public async Task<jewelry.Model.Stock.Product.PriceReviewAck.Response> Ack(jewelry.Model.Stock.Product.PriceReviewAck.AckRequest request)
        {
            CheckPermissionLevel("update_stock");

            var stockNumbers = NormalizeStockNumbers(request?.StockNumbers);
            if (request?.Note != null && request.Note.Length > 500)
                throw new HandleException("หมายเหตุต้องไม่เกิน 500 ตัวอักษร");

            var prices = await _jewelryContext.TbtStockPiece.AsNoTracking()
                .Where(p => stockNumbers.Contains(p.StockNumber))
                .Select(p => new { p.StockNumber, Price = p.SkuCodeNavigation.DefaultPrice })
                .ToListAsync();
            var priceByStock = prices.ToDictionary(x => x.StockNumber, x => x.Price ?? 0m);
            var missing = stockNumbers.Where(s => !priceByStock.ContainsKey(s)).ToList();
            if (missing.Count > 0)
                throw new HandleException($"ไม่พบสินค้า: {string.Join(", ", missing.Take(5))}");

            var now = DateTime.UtcNow;
            var previous = await _jewelryContext.TbtStockPriceReviewAck
                .Where(a => a.IsActive && stockNumbers.Contains(a.StockNumber))
                .ToListAsync();
            foreach (var a in previous)
            {
                a.IsActive = false;
                a.UpdateBy = CurrentUsername;
                a.UpdateDate = now;
            }
            if (previous.Count > 0) _jewelryContext.TbtStockPriceReviewAck.UpdateRange(previous);

            var note = string.IsNullOrWhiteSpace(request?.Note) ? null : request!.Note!.Trim();
            _jewelryContext.TbtStockPriceReviewAck.AddRange(stockNumbers.Select(s => new TbtStockPriceReviewAck
            {
                StockNumber = s,
                PriceAcked = priceByStock[s],
                Note = note,
                IsActive = true,
                CreateBy = CurrentUsername,
                CreateDate = now
            }));
            await _jewelryContext.SaveChangesAsync();

            return new jewelry.Model.Stock.Product.PriceReviewAck.Response { Count = stockNumbers.Count };
        }

        public async Task<jewelry.Model.Stock.Product.PriceReviewAck.Response> Unack(jewelry.Model.Stock.Product.PriceReviewAck.UnackRequest request)
        {
            CheckPermissionLevel("update_stock");

            var stockNumbers = NormalizeStockNumbers(request?.StockNumbers);
            var now = DateTime.UtcNow;
            var rows = await _jewelryContext.TbtStockPriceReviewAck
                .Where(a => a.IsActive && stockNumbers.Contains(a.StockNumber))
                .ToListAsync();
            foreach (var a in rows)
            {
                a.IsActive = false;
                a.UpdateBy = CurrentUsername;
                a.UpdateDate = now;
            }
            if (rows.Count > 0)
            {
                _jewelryContext.TbtStockPriceReviewAck.UpdateRange(rows);
                await _jewelryContext.SaveChangesAsync();
            }
            return new jewelry.Model.Stock.Product.PriceReviewAck.Response { Count = rows.Select(r => r.StockNumber).Distinct().Count() };
        }

        private static List<string> NormalizeStockNumbers(List<string>? input)
        {
            var list = (input ?? new List<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();
            if (list.Count == 0) throw new HandleException("กรุณาระบุเลขสต็อก");
            if (list.Count > MaxItems) throw new HandleException($"Maximum {MaxItems} items per request");
            return list;
        }

        private static bool HasAnyFilter(jewelry.Model.Stock.Product.List.Search f)
        {
            foreach (var prop in typeof(jewelry.Model.Stock.Product.List.Search).GetProperties())
            {
                if (prop.Name == nameof(jewelry.Model.Stock.Product.List.Search.IncludeLastMovement)
                    || prop.Name == nameof(jewelry.Model.Stock.Product.List.Search.MaterialMatchAll)) continue;
                var v = prop.GetValue(f);
                if (v == null) continue;
                if (v is string str) { if (!string.IsNullOrWhiteSpace(str)) return true; continue; }
                if (v is System.Collections.IEnumerable en)
                {
                    foreach (var item in en)
                        if (item != null && !(item is string s2 && string.IsNullOrWhiteSpace(s2))) return true;
                    continue;
                }
                return true;
            }
            return false;
        }

        public async Task<Response> Check(Request request)
        {
            var items = request?.Items ?? new List<Item>();
            if (items.Count > MaxItems)
                throw new HandleException($"Maximum {MaxItems} items per request");

            var snap = await GetSnapshot();
            var contexts = await BuildContexts(items);

            var response = new Response();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var ctx = contexts[i];
                var reasons = PriceSanityEvaluator.Evaluate(snap, ctx, item.Price);
                var result = new ResultItem
                {
                    Key = item.Key,
                    IsOutlier = reasons.Count > 0,
                    Reasons = reasons
                };
                if (result.IsOutlier)
                    result.SuggestedPrice = PriceSanityEvaluator.Suggest(snap, ctx, item.Price);
                response.Items.Add(result);
            }
            return response;
        }

        private async Task<List<PriceSanityContext>> BuildContexts(List<Item> items)
        {
            var stockNumbers = items.Where(x => !string.IsNullOrWhiteSpace(x.StockNumber))
                .Select(x => x.StockNumber!.Trim()).Distinct().ToList();

            var pieces = new List<PieceRow>();
            var materials = new List<MaterialRow>();
            if (stockNumbers.Count > 0)
            {
                pieces = await _jewelryContext.TbtStockPiece.AsNoTracking()
                    .Where(p => stockNumbers.Contains(p.StockNumber))
                    .Select(p => new PieceRow { StockNumber = p.StockNumber, ProductCode = p.ProductCode, SkuCode = p.SkuCode, Wo = p.Wo, WoNumber = p.WoNumber })
                    .ToListAsync();

                materials = await _jewelryContext.TbtStockPieceMaterial.AsNoTracking()
                    .Where(m => stockNumbers.Contains(m.StockNumber))
                    .GroupBy(m => new { m.StockNumber, m.ProductCode })
                    .Select(g => new MaterialRow
                    {
                        StockNumber = g.Key.StockNumber,
                        ProductCode = g.Key.ProductCode,
                        Gold = g.Sum(x => x.Type.ToLower() == "gold" ? (x.Weight ?? 0m) : 0m),
                        Gem = g.Sum(x => x.Type.ToLower() != "gold" ? (x.Price ?? 0m) : 0m)
                    })
                    .ToListAsync();
            }

            var pieceByStock = pieces.GroupBy(p => p.StockNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var matByStock = materials.GroupBy(m => m.StockNumber, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key,
                    g => (Gold: g.Sum(x => x.Gold), Gem: g.Sum(x => x.Gem)),
                    StringComparer.OrdinalIgnoreCase);

            var skuCodes = items.Select(x => x.SkuCode)
                .Concat(pieces.Select(p => p.SkuCode))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim()).Distinct().ToList();

            var skuByCode = new Dictionary<string, SkuRow>(StringComparer.OrdinalIgnoreCase);
            if (skuCodes.Count > 0)
            {
                var skus = await _jewelryContext.TbtSku.AsNoTracking()
                    .Where(s => skuCodes.Contains(s.SkuCode))
                    .Select(s => new SkuRow
                    {
                        SkuCode = s.SkuCode,
                        DefaultPrice = s.DefaultPrice,
                        MoldDesign = s.MoldDesign,
                        ProductTypeName = s.ProductTypeName,
                        ProductionType = s.ProductionType
                    })
                    .ToListAsync();
                skuByCode = skus.ToDictionary(s => s.SkuCode, StringComparer.OrdinalIgnoreCase);
            }

            var list = new List<PriceSanityContext>();
            foreach (var item in items)
            {
                var stockNumber = item.StockNumber?.Trim();
                PieceRow? piece = null;
                var hasMat = false;
                (decimal Gold, decimal Gem) mat = default;
                if (!string.IsNullOrEmpty(stockNumber))
                {
                    pieceByStock.TryGetValue(stockNumber, out piece);
                    hasMat = matByStock.TryGetValue(stockNumber, out mat);
                }
                var skuCode = !string.IsNullOrWhiteSpace(item.SkuCode) ? item.SkuCode.Trim() : piece?.SkuCode;
                SkuRow? sku = null;
                if (skuCode != null) skuByCode.TryGetValue(skuCode, out sku);

                list.Add(new PriceSanityContext
                {
                    StockNumber = stockNumber,
                    SkuCode = skuCode,
                    Wo = !string.IsNullOrWhiteSpace(item.Wo) ? item.Wo : piece?.Wo,
                    WoNumber = item.WoNumber ?? piece?.WoNumber,
                    MoldDesign = !string.IsNullOrWhiteSpace(item.MoldDesign) ? item.MoldDesign : sku?.MoldDesign,
                    ProductTypeName = !string.IsNullOrWhiteSpace(item.ProductTypeName) ? item.ProductTypeName : sku?.ProductTypeName,
                    ProductionType = !string.IsNullOrWhiteSpace(item.ProductionType) ? item.ProductionType : sku?.ProductionType,
                    GoldWeight = item.GoldWeight ?? (hasMat ? mat.Gold : 0m),
                    GemPrice = item.GemPrice ?? (hasMat ? mat.Gem : 0m),
                    OldPrice = sku?.DefaultPrice ?? 0m
                });
            }
            return list;
        }

        private async Task<PriceSanitySnapshot> GetSnapshot()
        {
            if (_cache.TryGetValue(CacheKey, out PriceSanitySnapshot? cached) && cached != null)
                return cached;

            await BuildLock.WaitAsync();
            try
            {
                if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
                    return cached;

                var snap = (await BuildSnapshot()).Snap;
                _cache.Set(CacheKey, snap, CacheDuration);
                return snap;
            }
            finally
            {
                BuildLock.Release();
            }
        }

        private async Task<SnapshotBuild> BuildSnapshot()
        {
            var pieces = await _jewelryContext.TbtStockPiece.AsNoTracking()
                .Where(p => p.Status == "IN_STOCK")
                .Select(p => new PieceRow { StockNumber = p.StockNumber, ProductCode = p.ProductCode, SkuCode = p.SkuCode, Wo = p.Wo, WoNumber = p.WoNumber, StockNumberOrigin = p.StockNumberOrigin })
                .ToListAsync();

            var skus = await _jewelryContext.TbtSku.AsNoTracking()
                .Where(s => s.DefaultPrice > 0)
                .Select(s => new SkuRow
                {
                    SkuCode = s.SkuCode,
                    DefaultPrice = s.DefaultPrice,
                    MoldDesign = s.MoldDesign,
                    ProductTypeName = s.ProductTypeName,
                    ProductionType = s.ProductionType,
                    ProductNumber = s.ProductNumber,
                    ProductNameTh = s.ProductNameTh,
                    ImagePath = s.ImagePath
                })
                .ToListAsync();
            var skuByCode = skus.ToDictionary(s => s.SkuCode, StringComparer.OrdinalIgnoreCase);

            var materials = await _jewelryContext.TbtStockPieceMaterial.AsNoTracking()
                .Where(m => m.StockPieceNavigation.Status == "IN_STOCK")
                .GroupBy(m => new { m.StockNumber, m.ProductCode })
                .Select(g => new MaterialRow
                {
                    StockNumber = g.Key.StockNumber,
                    ProductCode = g.Key.ProductCode,
                    Gold = g.Sum(x => x.Type.ToLower() == "gold" ? (x.Weight ?? 0m) : 0m),
                    Gem = g.Sum(x => x.Type.ToLower() != "gold" ? (x.Price ?? 0m) : 0m)
                })
                .ToListAsync();
            var matByKey = materials.ToDictionary(m => m.StockNumber + "|" + m.ProductCode, m => (m.Gold, m.Gem));

            var snap = new PriceSanitySnapshot();
            var groupValues = new Dictionary<string, List<double>>();
            var allValues = new List<double>();
            var typeSkus = new Dictionary<string, Dictionary<string, decimal>>(StringComparer.OrdinalIgnoreCase);
            var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in pieces)
            {
                if (!skuByCode.TryGetValue(p.SkuCode, out var sku)) continue;
                var price = sku.DefaultPrice ?? 0m;
                if (price <= 0) continue;

                if (!string.IsNullOrWhiteSpace(p.Wo))
                {
                    var wo = p.Wo.Trim();
                    if (!snap.WoPieces.TryGetValue(wo, out var wl))
                        snap.WoPieces[wo] = wl = new List<WoPiece>();
                    wl.Add(new WoPiece { StockNumber = p.StockNumber, Sku = p.SkuCode, WoNumber = p.WoNumber, Price = price });
                }

                if (!string.IsNullOrWhiteSpace(sku.ProductTypeName))
                {
                    var t = sku.ProductTypeName.Trim();
                    typeCounts[t] = typeCounts.GetValueOrDefault(t) + 1;
                    if (!typeSkus.TryGetValue(t, out var d))
                        typeSkus[t] = d = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    d[p.SkuCode] = price;
                }

                if (matByKey.TryGetValue(p.StockNumber + "|" + p.ProductCode, out var mat) && mat.Gold > 0)
                {
                    var cost = PriceSanityEvaluator.Cost(mat.Gold, mat.Gem, sku.ProductionType);
                    if (cost > 0)
                    {
                        var lr = Math.Log((double)price / cost);
                        var key = PriceSanityEvaluator.NormalizeGroup(sku.ProductionType);
                        if (!groupValues.TryGetValue(key, out var gl))
                            groupValues[key] = gl = new List<double>();
                        gl.Add(lr);
                        allValues.Add(lr);
                    }
                }
            }

            snap.GlobalStats = PriceSanityEvaluator.BuildStats(allValues);
            foreach (var kv in groupValues)
                snap.GroupStats[kv.Key] = PriceSanityEvaluator.BuildStats(kv.Value);

            foreach (var kv in typeSkus)
            {
                snap.TypeCeilings[kv.Key] = new TypeCeilingInfo
                {
                    N = typeCounts[kv.Key],
                    Top = kv.Value.OrderByDescending(x => x.Value).Take(2).Select(x => (x.Key, x.Value)).ToList()
                };
            }

            foreach (var s in skus)
            {
                if (string.IsNullOrWhiteSpace(s.MoldDesign)) continue;
                var md = s.MoldDesign.Trim();
                if (!snap.DesignSkus.TryGetValue(md, out var dl))
                    snap.DesignSkus[md] = dl = new List<DesignSku>();
                dl.Add(new DesignSku { Sku = s.SkuCode, Price = s.DefaultPrice ?? 0m });
            }

            return new SnapshotBuild { Snap = snap, Pieces = pieces, SkuByCode = skuByCode, MatByKey = matByKey };
        }

        private class SnapshotBuild
        {
            public PriceSanitySnapshot Snap { get; set; } = null!;
            public List<PieceRow> Pieces { get; set; } = new();
            public Dictionary<string, SkuRow> SkuByCode { get; set; } = new();
            public Dictionary<string, (decimal Gold, decimal Gem)> MatByKey { get; set; } = new();
        }

        private class PieceRow
        {
            public string StockNumber { get; set; } = null!;
            public string ProductCode { get; set; } = null!;
            public string SkuCode { get; set; } = null!;
            public string? Wo { get; set; }
            public int? WoNumber { get; set; }
            public string? StockNumberOrigin { get; set; }
        }

        private class MaterialRow
        {
            public string StockNumber { get; set; } = null!;
            public string ProductCode { get; set; } = null!;
            public decimal Gold { get; set; }
            public decimal Gem { get; set; }
        }

        private class SkuRow
        {
            public string SkuCode { get; set; } = null!;
            public decimal? DefaultPrice { get; set; }
            public string? MoldDesign { get; set; }
            public string? ProductTypeName { get; set; }
            public string? ProductionType { get; set; }
            public string? ProductNumber { get; set; }
            public string? ProductNameTh { get; set; }
            public string? ImagePath { get; set; }
        }
    }
}
