using jewelry.Model.Exceptions;
using jewelry.Model.Stock.Product.PriceCheck;
using Jewelry.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Jewelry.Service.Stock.PriceSanity
{
    public class PriceSanityService : IPriceSanityService
    {
        private const int MaxItems = 500;
        private const string CacheKey = "PriceSanity:Snapshot";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);
        private static readonly SemaphoreSlim BuildLock = new(1, 1);

        private readonly JewelryContext _jewelryContext;
        private readonly IMemoryCache _cache;

        public PriceSanityService(JewelryContext jewelryContext, IMemoryCache cache)
        {
            _jewelryContext = jewelryContext;
            _cache = cache;
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

                var snap = await BuildSnapshot();
                _cache.Set(CacheKey, snap, CacheDuration);
                return snap;
            }
            finally
            {
                BuildLock.Release();
            }
        }

        private async Task<PriceSanitySnapshot> BuildSnapshot()
        {
            var pieces = await _jewelryContext.TbtStockPiece.AsNoTracking()
                .Where(p => p.Status == "IN_STOCK")
                .Select(p => new PieceRow { StockNumber = p.StockNumber, ProductCode = p.ProductCode, SkuCode = p.SkuCode, Wo = p.Wo, WoNumber = p.WoNumber })
                .ToListAsync();

            var skus = await _jewelryContext.TbtSku.AsNoTracking()
                .Where(s => s.DefaultPrice > 0)
                .Select(s => new SkuRow
                {
                    SkuCode = s.SkuCode,
                    DefaultPrice = s.DefaultPrice,
                    MoldDesign = s.MoldDesign,
                    ProductTypeName = s.ProductTypeName,
                    ProductionType = s.ProductionType
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

            return snap;
        }

        private class PieceRow
        {
            public string StockNumber { get; set; } = null!;
            public string ProductCode { get; set; } = null!;
            public string SkuCode { get; set; } = null!;
            public string? Wo { get; set; }
            public int? WoNumber { get; set; }
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
        }
    }
}
